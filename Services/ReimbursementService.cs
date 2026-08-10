using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services.Storage;
using SimplexLawFirm.ViewModels;

namespace SimplexLawFirm.Services;

public interface IReimbursementService
{
    Task<ReimbursementClaim> BeginAsync(int attorneyId, BeginReimbursementViewModel input, CancellationToken ct = default);
    Task<ReimbursementClaim> SubmitProofAsync(int attorneyId, int claimId, IFormFile proof, CancellationToken ct = default);
    Task DecideAsync(int claimId, int directorId, bool approve, string reason, CancellationToken ct = default);
}

public sealed class ReimbursementService(
    ApplicationDbContext db,
    IReimbursementProofStorage storage,
    INotificationService notifications) : IReimbursementService
{
    private static readonly EventType[] SupportedEvents =
    [
        EventType.Appointment, EventType.CourtAppearance, EventType.Hearing,
        EventType.Consultation, EventType.Deposition, EventType.Mediation
    ];

    public async Task<ReimbursementClaim> BeginAsync(int attorneyId, BeginReimbursementViewModel input, CancellationToken ct = default)
    {
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == input.CaseId, ct)
            ?? throw new InvalidOperationException("Matter not found.");
        if (matter.LawyerId != attorneyId) throw new UnauthorizedAccessException("Only the attorney assigned to this matter may claim reimbursement.");
        if (matter.Status != CaseStatus.Active) throw new InvalidOperationException("Reimbursement claims can only be made against an active matter.");
        if (input.ExpenseDate.Date > DateTime.Today) throw new InvalidOperationException("The expense date cannot be in the future.");

        var claim = new ReimbursementClaim
        {
            ClaimNumber = $"RMB-{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
            CaseId = matter.Id,
            AttorneyId = attorneyId,
            ExpenseType = input.ExpenseType,
            ExpenseDate = input.ExpenseDate.Date,
            Amount = input.Amount,
            Description = input.Description.Trim(),
            Status = ReimbursementStatus.DraftProofRequired
        };

        var dayStart = input.ExpenseDate.Date;
        var dayEnd = dayStart.AddDays(1);
        var calendarActivity = await db.CalendarEvents
            .Where(x => x.CaseId == matter.Id && x.StartDateTime >= dayStart && x.StartDateTime < dayEnd
                && (x.Status == EventStatus.Completed || x.ActualStartTime != null)
                && SupportedEvents.Contains(x.Type))
            .OrderBy(x => x.StartDateTime)
            .Select(x => new { x.Id, x.Type })
            .FirstOrDefaultAsync(ct);
        if (calendarActivity != null)
        {
            claim.MatchedActivityId = calendarActivity.Id;
            claim.MatchedActivityType = calendarActivity.Type is EventType.CourtAppearance or EventType.Hearing
                ? ReimbursementActivityType.CourtEvent
                : ReimbursementActivityType.Appointment;
        }
        else
        {
            var timeEntryId = await db.TimeEntries
                .Where(x => x.CaseId == matter.Id && x.LawyerId == attorneyId && x.Date >= dayStart && x.Date < dayEnd && x.Hours > 0)
                .OrderBy(x => x.Date)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct);
            if (timeEntryId.HasValue)
            {
                claim.MatchedActivityId = timeEntryId;
                claim.MatchedActivityType = ReimbursementActivityType.TimeEntry;
            }
            else
            {
                claim.Status = ReimbursementStatus.RefusedValidation;
                claim.ValidationFailureReason = "No appointment, court event, or logged working hours were found for this matter on the expense date.";
            }
        }

        db.ReimbursementClaims.Add(claim);
        await db.SaveChangesAsync(ct);
        AddAudit(claim, claim.Status == ReimbursementStatus.RefusedValidation ? "Validation refused" : "Activity verified",
            claim.ValidationFailureReason ?? $"Matched {claim.MatchedActivityType} #{claim.MatchedActivityId}.", attorneyId);
        await db.SaveChangesAsync(ct);
        return claim;
    }

    public async Task<ReimbursementClaim> SubmitProofAsync(int attorneyId, int claimId, IFormFile proof, CancellationToken ct = default)
    {
        var claim = await db.ReimbursementClaims.Include(x => x.Case)
            .SingleOrDefaultAsync(x => x.Id == claimId, ct) ?? throw new InvalidOperationException("Claim not found.");
        if (claim.AttorneyId != attorneyId || claim.Case.LawyerId != attorneyId) throw new UnauthorizedAccessException("You cannot submit proof for this claim.");
        if (claim.Status != ReimbursementStatus.DraftProofRequired) throw new InvalidOperationException("This claim is no longer awaiting proof.");

        var stored = await storage.StoreAsync(claim.Id, proof, ct);
        claim.ProofOriginalFileName = stored.OriginalFileName;
        claim.ProofRelativePath = stored.RelativePath;
        claim.ProofContentType = stored.ContentType;
        claim.ProofSizeBytes = stored.SizeBytes;
        claim.ProofSha256Hash = stored.Sha256Hash;

        var duplicate = await db.ReimbursementClaims.AnyAsync(x => x.Id != claim.Id
            && x.CaseId == claim.CaseId
            && x.AttorneyId == claim.AttorneyId
            && x.Status != ReimbursementStatus.RefusedValidation
            && ((x.ProofSha256Hash != null && x.ProofSha256Hash == stored.Sha256Hash)
                || (x.ExpenseType == claim.ExpenseType && x.ExpenseDate == claim.ExpenseDate && x.Amount == claim.Amount)), ct);
        if (duplicate)
        {
            await storage.DeleteAsync(stored.RelativePath, ct);
            claim.ProofOriginalFileName = null;
            claim.ProofRelativePath = null;
            claim.ProofContentType = null;
            claim.ProofSizeBytes = null;
            claim.ProofSha256Hash = null;
            claim.Status = ReimbursementStatus.RefusedValidation;
            claim.ValidationFailureReason = "This expense duplicates a reimbursement claim already submitted for the matter.";
            AddAudit(claim, "Duplicate refused", claim.ValidationFailureReason, attorneyId);
            await db.SaveChangesAsync(ct);
            return claim;
        }

        var policy = await db.ExpensePolicies.SingleOrDefaultAsync(x => x.ExpenseType == claim.ExpenseType && x.IsActive, ct)
            ?? throw new InvalidOperationException($"No active expense policy is configured for {claim.ExpenseType}.");
        claim.PolicyLimitSnapshot = policy.PerItemLimit;
        claim.DelegatedLimitSnapshot = policy.DelegatedApprovalLimit;
        claim.ExceedsPolicyLimit = claim.Amount > policy.PerItemLimit;
        var matterTerm = await db.MatterExpenseTerms.SingleOrDefaultAsync(x => x.CaseId == claim.CaseId && x.ExpenseType == claim.ExpenseType, ct);
        claim.Classification = matterTerm?.Classification ?? policy.DefaultClassification;
        claim.ClassificationReason = matterTerm == null
            ? $"Classified by the active {claim.ExpenseType} expense policy."
            : $"Matter-specific term: {matterTerm.Reason}";

        if (claim.Amount <= policy.DelegatedApprovalLimit && !claim.ExceedsPolicyLimit)
        {
            claim.Status = ReimbursementStatus.AutoApproved;
            claim.DecidedAtUtc = DateTime.UtcNow;
            claim.DecisionReason = "Automatically approved within the delegated and policy limits.";
            ApproveFinancialRecords(claim);
            AddAudit(claim, "Automatically approved", claim.DecisionReason, null);
        }
        else
        {
            claim.Status = ReimbursementStatus.PendingDirector;
            AddAudit(claim, "Routed to Director",
                claim.ExceedsPolicyLimit
                    ? $"Amount exceeds the R {policy.PerItemLimit:N2} policy limit."
                    : $"Amount exceeds the R {policy.DelegatedApprovalLimit:N2} delegated approval limit.", attorneyId);
            foreach (var directorId in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct))
                await notifications.QueueAsync(directorId, "ReimbursementApproval", "Reimbursement requires review",
                    $"{claim.ClaimNumber} for R {claim.Amount:N2} requires a Director decision.", $"/Reimbursement/Review/{claim.Id}",
                    $"reimbursement-review-{claim.Id}-{directorId}", ct);
        }
        await db.SaveChangesAsync(ct);
        return claim;
    }

    public async Task DecideAsync(int claimId, int directorId, bool approve, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) throw new InvalidOperationException("The Director must record a meaningful decision reason.");
        _ = await db.Users.SingleOrDefaultAsync(x => x.Id == directorId && x.Role == UserRole.Admin && x.IsActive, ct)
            ?? throw new UnauthorizedAccessException("Only an active Director may decide the claim.");
        var claim = await db.ReimbursementClaims.SingleOrDefaultAsync(x => x.Id == claimId, ct)
            ?? throw new InvalidOperationException("Claim not found.");
        if (claim.Status != ReimbursementStatus.PendingDirector) throw new InvalidOperationException("This claim is not awaiting a Director decision.");
        claim.Status = approve ? ReimbursementStatus.Approved : ReimbursementStatus.Refused;
        claim.DecidedAtUtc = DateTime.UtcNow;
        claim.DecidedByUserId = directorId;
        claim.DecisionReason = reason.Trim();
        if (approve) ApproveFinancialRecords(claim);
        AddAudit(claim, approve ? "Director approved" : "Director refused", claim.DecisionReason, directorId);
        await notifications.QueueAsync(claim.AttorneyId, "ReimbursementDecision", $"Reimbursement {claim.Status}",
            $"{claim.ClaimNumber}: {claim.DecisionReason}", $"/Reimbursement/Details/{claim.Id}",
            $"reimbursement-decision-{claim.Id}", ct);
        await db.SaveChangesAsync(ct);
    }

    private void ApproveFinancialRecords(ReimbursementClaim claim)
    {
        db.AttorneyReimbursementPayables.Add(new AttorneyReimbursementPayable
        {
            Claim = claim, AttorneyId = claim.AttorneyId, Amount = claim.Amount
        });
        if (claim.Classification == ExpenseClassification.ClientRecoverable)
            db.MatterDisbursements.Add(new MatterDisbursement
            {
                Claim = claim, CaseId = claim.CaseId, Amount = claim.Amount,
                Description = $"{claim.ExpenseType}: {claim.Description}", IncurredDate = claim.ExpenseDate
            });
    }

    private void AddAudit(ReimbursementClaim claim, string action, string details, int? actorId) =>
        db.ReimbursementAuditEntries.Add(new ReimbursementAuditEntry
        {
            Claim = claim, Action = action, Details = details, ActorUserId = actorId
        });
}
