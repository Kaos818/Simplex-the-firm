using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.ViewModels;

namespace SimplexLawFirm.Services;

public interface ILegalCostRecoveryService
{
    Task<LegalCostRecoveryClaim> SubmitAsync(int attorneyId, CreateLegalCostRecoveryViewModel input, CancellationToken ct = default);
    Task<LegalCostRecoveryClaim> DecideAsync(int directorId, DecideLegalCostRecoveryViewModel input, CancellationToken ct = default);
}

public sealed class LegalCostRecoveryService(ApplicationDbContext db, INotificationService notifications, IEmailService email) : ILegalCostRecoveryService
{
    public async Task<LegalCostRecoveryClaim> SubmitAsync(int attorneyId, CreateLegalCostRecoveryViewModel input, CancellationToken ct = default)
    {
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == input.CaseId, ct)
            ?? throw new KeyNotFoundException("Case not found.");
        if (matter.LawyerId != attorneyId) throw new UnauthorizedAccessException("Only the assigned attorney may claim wasted costs.");
        if (string.IsNullOrWhiteSpace(input.Justification) || input.Justification.Trim().Length < 20)
            throw new ArgumentException("A specific written justification is required.");
        if (string.IsNullOrWhiteSpace(input.OpposingPartyName) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(input.OpposingPartyEmail))
            throw new ArgumentException("Valid opposing-party service details are required.");
        var ids = input.TimeEntryIds.Distinct().ToArray();
        var entries = await db.TimeEntries.Where(x => ids.Contains(x.Id) && x.CaseId == matter.Id && x.LawyerId == attorneyId).ToListAsync(ct);
        if (entries.Count == 0 || entries.Count != ids.Length) throw new ArgumentException("Select valid working-hour entries logged by the assigned attorney.");
        var alreadyClaimed = await db.LegalCostRecoveryTimeEntries.AnyAsync(x => ids.Contains(x.TimeEntryId), ct);
        if (alreadyClaimed) throw new InvalidOperationException("A selected working-hour entry already supports another cost recovery claim.");
        var snapshots = entries.Select(x => new LegalCostRecoveryTimeEntry { TimeEntryId = x.Id, AmountSnapshot = x.TotalAmount }).ToList();
        var claim = new LegalCostRecoveryClaim
        {
            CaseId = matter.Id, AttorneyId = attorneyId, Ground = input.Ground,
            Justification = input.Justification.Trim(), OpposingPartyName = input.OpposingPartyName.Trim(), OpposingPartyEmail = input.OpposingPartyEmail.Trim(),
            ClaimedAmount = snapshots.Sum(x => x.AmountSnapshot), TimeEntries = snapshots
        };
        claim.AuditEntries.Add(new() { ActorUserId = attorneyId, Action = "Submitted", Details = $"Claim submitted for {entries.Count} logged working-hour entries." });
        db.LegalCostRecoveryClaims.Add(claim);
        await db.SaveChangesAsync(ct);
        var directors = await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct);
        foreach (var director in directors)
            await notifications.QueueAsync(director, "LegalCostRecoveryReview", "Wasted-cost claim requires review", $"Claim #{claim.Id} for R {claim.ClaimedAmount:N2} requires a director decision.", $"/LegalCostRecovery/Review/{claim.Id}", $"cost-recovery-review:{claim.Id}:{director}", ct);
        await db.SaveChangesAsync(ct);
        return claim;
    }

    public async Task<LegalCostRecoveryClaim> DecideAsync(int directorId, DecideLegalCostRecoveryViewModel input, CancellationToken ct = default)
    {
        var director = await db.Users.SingleOrDefaultAsync(x => x.Id == directorId, ct);
        if (director?.Role != UserRole.Admin) throw new UnauthorizedAccessException("Only a director may decide a cost recovery claim.");
        var claim = await db.LegalCostRecoveryClaims.Include(x => x.Attorney).Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == input.ClaimId, ct)
            ?? throw new KeyNotFoundException("Claim not found.");
        if (claim.Status != LegalCostRecoveryStatus.PendingDirectorApproval) throw new InvalidOperationException("This claim has already been decided.");
        if (string.IsNullOrWhiteSpace(input.DecisionNotes) || input.DecisionNotes.Trim().Length < 10) throw new ArgumentException("Decision notes are required.");
        if (input.Decision is not (LegalCostRecoveryStatus.ApprovedInFull or LegalCostRecoveryStatus.PartiallyAwarded or LegalCostRecoveryStatus.Rejected))
            throw new ArgumentException("Select a valid director decision.");
        decimal? award = input.Decision switch
        {
            LegalCostRecoveryStatus.ApprovedInFull => claim.ClaimedAmount,
            LegalCostRecoveryStatus.Rejected => 0,
            _ when input.AwardedAmount is > 0 && input.AwardedAmount <= claim.ClaimedAmount => input.AwardedAmount,
            _ => throw new ArgumentException("A partial award must be greater than zero and no more than the claimed amount.")
        };
        claim.Status = input.Decision;
        claim.AwardedAmount = award;
        claim.DecisionNotes = input.DecisionNotes.Trim();
        claim.DecidedByUserId = directorId;
        claim.DecidedAtUtc = DateTime.UtcNow;
        var shouldServe = input.Decision is LegalCostRecoveryStatus.ApprovedInFull or LegalCostRecoveryStatus.PartiallyAwarded;
        if (shouldServe)
        {
            claim.ServedAtUtc = DateTime.UtcNow;
            claim.ServiceDeliveryReference = $"COST-{claim.Id}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
            claim.Case.CostRecoveryAwardedTotal += award!.Value;
        }
        claim.AuditEntries.Add(new() { ActorUserId = directorId, Action = "DirectorDecisionRecorded", Details = $"{claim.Status}; awarded R {award:N2}. {claim.DecisionNotes}" });
        claim.AuditEntries.Add(new() { ActorUserId = directorId, Action = shouldServe ? "MarkedServed" : "NotServed", Details = shouldServe ? "Claim was served only after the director decision was recorded." : "Rejected claim was not served on the opposing party." });
        await notifications.QueueAsync(claim.AttorneyId, "LegalCostRecoveryDecision", $"Wasted-cost claim {claim.Status}", $"Claim #{claim.Id} was decided. Award: R {award:N2}.", $"/LegalCostRecovery/Details/{claim.Id}", $"cost-recovery-decision:{claim.Id}", ct);
        await email.QueueAsync(claim.Attorney.Email, $"Wasted-cost claim {claim.Status}", $"<p>Claim <strong>#{claim.Id}</strong> has been decided.</p><p>Award: <strong>R {award:N2}</strong></p><p>{System.Net.WebUtility.HtmlEncode(claim.DecisionNotes)}</p>", $"Claim #{claim.Id} was decided. Award: R {award:N2}. {claim.DecisionNotes}", $"cost-recovery-decision-email:{claim.Id}", ct);
        if (shouldServe)
            await email.QueueAsync(claim.OpposingPartyEmail, $"Legal cost recovery claim - {claim.CaseId}", $"<h2>Legal cost recovery claim</h2><p>Dear {System.Net.WebUtility.HtmlEncode(claim.OpposingPartyName)},</p><p>A Director decision has been recorded and the claim is now served.</p><p><strong>Ground:</strong> {claim.Ground}<br/><strong>Claimed:</strong> R {claim.ClaimedAmount:N2}<br/><strong>Awarded:</strong> R {award:N2}</p><p><strong>Decision:</strong> {claim.Status}</p><p>Service reference: <strong>{claim.ServiceDeliveryReference}</strong></p>", $"Legal cost recovery claim. Decision: {claim.Status}. Claimed R {claim.ClaimedAmount:N2}; awarded R {award:N2}. Service reference {claim.ServiceDeliveryReference}.", $"cost-recovery-service:{claim.Id}", ct);
        await db.SaveChangesAsync(ct);
        return claim;
    }
}
