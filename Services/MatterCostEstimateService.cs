using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.ViewModels;

namespace SimplexLawFirm.Services;

public interface IMatterCostEstimateService
{
    Task<CostEstimateEnquiry> BeginAsync(string matterType, int? clientId, CancellationToken ct = default);
    Task<MatterCostEstimate> CalculateAndLockAsync(int enquiryId, CreateCostEstimateViewModel input, CancellationToken ct = default);
    Task LinkToMatterAsync(int estimateId, int caseId, CancellationToken ct = default);
    Task TryAutoLinkAsync(int caseId, CancellationToken ct = default);
    Task<InvoiceEstimateAuthorisation?> EvaluateInvoiceAsync(Invoice invoice, CancellationToken ct = default);
    Task ApproveVarianceAsync(int invoiceId, int directorId, string reason, CancellationToken ct = default);
}

public sealed class MatterCostEstimateService(ApplicationDbContext db, INotificationService notifications) : IMatterCostEstimateService
{
    public const int MinimumComparableMatters = 3;
    public const decimal VatRate = .15m;

    public async Task<CostEstimateEnquiry> BeginAsync(string matterType, int? clientId, CancellationToken ct = default)
    {
        matterType = matterType?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(matterType)) throw new InvalidOperationException("Select a matter type.");
        var enquiry = new CostEstimateEnquiry { MatterType = matterType, ClientId = clientId };
        if (clientId.HasValue)
        {
            var client = await db.Clients.SingleAsync(x => x.Id == clientId, ct);
            enquiry.ContactName = client.FullName ?? "";
            enquiry.Email = client.Email;
        }
        db.CostEstimateEnquiries.Add(enquiry);
        await db.SaveChangesAsync(ct);
        var count = await ComparableCases(matterType).CountAsync(ct);
        var usableCount = await ComparableCases(matterType)
            .CountAsync(matter => db.TimeEntries.Any(entry => entry.CaseId == matter.Id && entry.IsBillable && entry.Hours > 0)
                || db.Invoices.Any(invoice => invoice.CaseId == matter.Id
                    && invoice.Status != InvoiceStatus.Cancelled
                    && invoice.TotalAmount > 0), ct);
        var hasRates = await db.LawyerProfiles.AnyAsync(x => x.IsActive && x.HourlyRate > 0, ct);
        if (usableCount < MinimumComparableMatters || !hasRates)
        {
            var reason = !hasRates
                ? "Current attorney charge-out rates are not maintained."
                : $"Only {usableCount} of {count} comparable closed matter(s) contain usable time or billing history; at least {MinimumComparableMatters} are required.";
            db.MatterCostEstimates.Add(new MatterCostEstimate { CostEstimateEnquiryId = enquiry.Id, Status = CostEstimateStatus.Declined, ComparableMatterCount = usableCount, DeclineReason = reason });
            db.CostEstimateCoverageGaps.Add(new CostEstimateCoverageGap { MatterType = matterType, ComparableMatterCount = usableCount, Reason = reason, Status = EstimateGapStatus.Open });
            foreach (var admin in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct))
                await notifications.QueueAsync(admin, "EstimateCoverageGap", "Cost estimate coverage gap", $"{matterType}: {reason}", "/CostEstimate/Admin", $"estimate-gap-{enquiry.Id}-{admin}", ct);
            await db.SaveChangesAsync(ct);
        }
        return enquiry;
    }

    public async Task<MatterCostEstimate> CalculateAndLockAsync(int enquiryId, CreateCostEstimateViewModel input, CancellationToken ct = default)
    {
        var enquiry = await db.CostEstimateEnquiries.Include(x => x.Estimates).SingleAsync(x => x.Id == enquiryId, ct);
        if (enquiry.Estimates.Any()) throw new InvalidOperationException("This enquiry already has a final estimate or recorded refusal.");
        var rates = await db.LawyerProfiles.Include(x => x.User).Where(x => x.IsActive && x.HourlyRate > 0).OrderBy(x => x.Id).ToListAsync(ct);
        var cases = await ComparableCases(enquiry.MatterType).Include(x => x.Documents).ToListAsync(ct);
        if (rates.Count == 0 || cases.Count < MinimumComparableMatters) throw new InvalidOperationException("The estimate no longer has an adequate live basis.");

        enquiry.ContactName = input.ContactName.Trim();
        enquiry.Email = input.Email.Trim();
        enquiry.MatterValue = input.MatterValue;
        enquiry.Urgency = input.Urgency;
        enquiry.RequiresCourtProceedings = input.RequiresCourtProceedings;
        enquiry.DocumentReadiness = input.DocumentReadiness;

        var weightedRate = rates.Average(x => x.HourlyRate);
        var snapshots = new List<EstimateComparableSnapshot>();
        foreach (var matter in cases)
        {
            var hours = await db.TimeEntries.Where(x => x.CaseId == matter.Id && x.IsBillable).SumAsync(x => (decimal?)x.Hours, ct) ?? 0;
            var billed = await db.Invoices.Where(x => x.CaseId == matter.Id && x.Status != InvoiceStatus.Cancelled).SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;
            if (hours <= 0 && billed > 0) hours = billed / weightedRate;
            snapshots.Add(new EstimateComparableSnapshot(matter.CaseNumber, hours, billed));
        }
        var usable = snapshots.Where(x => x.Hours > 0 || x.BilledTotal > 0).ToList();
        if (usable.Count < MinimumComparableMatters) throw new InvalidOperationException("Comparable matters do not contain enough cost history to support a quote.");

        var averageHours = usable.Average(x => x.Hours > 0 ? x.Hours : x.BilledTotal / weightedRate);
        var valueMultiplier = input.MatterValue >= 5_000_000 ? 1.30m : input.MatterValue >= 1_000_000 ? 1.15m : 1m;
        var urgencyMultiplier = input.Urgency switch { MatterUrgency.Urgent => 1.20m, MatterUrgency.Immediate => 1.40m, _ => 1m };
        var courtMultiplier = input.RequiresCourtProceedings ? 1.45m : 1m;
        var documentsMultiplier = input.DocumentReadiness switch { DocumentReadiness.Complete => .90m, DocumentReadiness.Limited => 1.20m, _ => 1m };
        var complexity = valueMultiplier * urgencyMultiplier * courtMultiplier * documentsMultiplier;
        var professionalLow = RoundMoney(averageHours * .80m * weightedRate * complexity);
        var professionalHigh = RoundMoney(averageHours * 1.25m * weightedRate * complexity);
        var baseDisbursement = input.RequiresCourtProceedings ? 7_500m : 2_000m;
        var valueDisbursement = Math.Min(input.MatterValue * .0025m, 25_000m);
        var disbursementLow = RoundMoney((baseDisbursement + valueDisbursement) * .80m);
        var disbursementHigh = RoundMoney((baseDisbursement + valueDisbursement) * 1.20m);
        var vatLow = RoundMoney((professionalLow + disbursementLow) * VatRate);
        var vatHigh = RoundMoney((professionalHigh + disbursementHigh) * VatRate);
        var estimate = new MatterCostEstimate
        {
            CostEstimateEnquiryId = enquiry.Id, Status = CostEstimateStatus.Locked, ComparableMatterCount = usable.Count,
            ProfessionalFeesLow = professionalLow, ProfessionalFeesHigh = professionalHigh,
            DisbursementsLow = disbursementLow, DisbursementsHigh = disbursementHigh,
            VatLow = vatLow, VatHigh = vatHigh, TotalLow = professionalLow + disbursementLow + vatLow, TotalHigh = professionalHigh + disbursementHigh + vatHigh,
            RatesSnapshotJson = JsonSerializer.Serialize(rates.Select(x => new EstimateRateSnapshot(x.Id, x.User.FullName, x.HourlyRate))),
            AssumptionsSnapshotJson = JsonSerializer.Serialize(new EstimateAssumptions(averageHours, weightedRate, valueMultiplier, urgencyMultiplier, courtMultiplier, documentsMultiplier, VatRate)),
            ComparableMattersSnapshotJson = JsonSerializer.Serialize(usable)
        };
        db.MatterCostEstimates.Add(estimate);
        await db.SaveChangesAsync(ct);
        foreach (var admin in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct))
            await notifications.QueueAsync(admin, "CostEstimateEnquiry", "New cost estimate enquiry", $"{enquiry.ContactName} requested a {enquiry.MatterType} estimate.", $"/CostEstimate/AdminDetails/{estimate.Id}", $"estimate-enquiry-{estimate.Id}-{admin}", ct);
        await db.SaveChangesAsync(ct);
        return estimate;
    }

    public async Task LinkToMatterAsync(int estimateId, int caseId, CancellationToken ct = default)
    {
        var estimate = await db.MatterCostEstimates.Include(x => x.Enquiry).SingleAsync(x => x.Id == estimateId && x.Status == CostEstimateStatus.Locked, ct);
        var matter = await db.Cases.Include(x => x.Client).SingleAsync(x => x.Id == caseId, ct);
        if (matter.Status != CaseStatus.Active)
            throw new InvalidOperationException("A locked estimate can only be linked to an active instructed matter.");
        if (!string.Equals(estimate.Enquiry.Email.Trim(), matter.Client.Email.Trim(), StringComparison.OrdinalIgnoreCase) || !string.Equals(estimate.Enquiry.MatterType.Trim(), matter.CaseType.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The estimate client and matter type must match the instructed matter.");
        estimate.LinkedCaseId = matter.Id; estimate.LinkedAtUtc = DateTime.UtcNow; estimate.Status = CostEstimateStatus.Linked;
        await db.SaveChangesAsync(ct);
    }

    public async Task TryAutoLinkAsync(int caseId, CancellationToken ct = default)
    {
        var matter = await db.Cases.Include(x => x.Client).SingleAsync(x => x.Id == caseId, ct);
        var estimate = await db.MatterCostEstimates.Include(x => x.Enquiry)
            .Where(x => x.Status == CostEstimateStatus.Locked && x.LinkedCaseId == null
                && x.Enquiry.Email.ToUpper() == matter.Client.Email.ToUpper()
                && x.Enquiry.MatterType.ToUpper() == matter.CaseType.ToUpper())
            .OrderByDescending(x => x.LockedAtUtc).FirstOrDefaultAsync(ct);
        if (estimate != null) await LinkToMatterAsync(estimate.Id, matter.Id, ct);
    }

    public async Task<InvoiceEstimateAuthorisation?> EvaluateInvoiceAsync(Invoice invoice, CancellationToken ct = default)
    {
        if (!invoice.CaseId.HasValue) return null;
        var estimate = await db.MatterCostEstimates.Where(x => x.LinkedCaseId == invoice.CaseId && x.Status == CostEstimateStatus.Linked).OrderByDescending(x => x.LinkedAtUtc).FirstOrDefaultAsync(ct);
        if (estimate == null) return null;
        invoice.MatterCostEstimateId = estimate.Id;
        var total = invoice.TotalAmount;
        var variance = total > estimate.TotalHigh ? (total - estimate.TotalHigh) / estimate.TotalHigh :
            total < estimate.TotalLow ? (total - estimate.TotalLow) / estimate.TotalLow : 0;
        if (Math.Abs(variance) <= estimate.PermittedVarianceTolerance) return null;
        invoice.RequiresEstimateAuthorisation = true;
        invoice.Status = InvoiceStatus.Draft;
        var authorisation = new InvoiceEstimateAuthorisation { Invoice = invoice, MatterCostEstimateId = estimate.Id, InvoiceTotal = total, VariancePercent = variance };
        db.InvoiceEstimateAuthorisations.Add(authorisation);
        foreach (var admin in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct))
            await notifications.QueueAsync(admin, "InvoiceEstimateVariance", "Invoice exceeds estimate tolerance", $"An invoice for matter {invoice.CaseId} varies from the locked estimate by {variance:P1}.", "/Billing/AdminInvoices", $"invoice-estimate-variance-{invoice.InvoiceNumber}-{admin}", ct);
        return authorisation;
    }

    public async Task ApproveVarianceAsync(int invoiceId, int directorId, string reason, CancellationToken ct = default)
    {
        var director = await db.Users.SingleAsync(x => x.Id == directorId && x.Role == UserRole.Admin && x.IsActive, ct);
        var authorisation = await db.InvoiceEstimateAuthorisations.Include(x => x.Invoice).SingleAsync(x => x.InvoiceId == invoiceId && !x.IsApproved, ct);
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("A Director must record the reason for authorisation.");
        authorisation.IsApproved = true; authorisation.ApprovedByUserId = director.Id; authorisation.ApprovedAtUtc = DateTime.UtcNow; authorisation.ApprovalReason = reason.Trim();
        authorisation.Invoice.RequiresEstimateAuthorisation = false;
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<Case> ComparableCases(string matterType) =>
        db.Cases.Where(x => x.Status == CaseStatus.Closed && x.CaseType == matterType);
    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
