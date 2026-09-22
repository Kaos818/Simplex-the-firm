using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;

namespace SimplexLawFirm.Services;

public interface IAdverseCostExposureService
{
    Task<AdverseCostExposure> LogAsync(AdverseCostExposure input, int attorneyId, CancellationToken ct = default);
    Task UpdateProgressAsync(int exposureId, int attorneyId, AdverseCostExposureStatus status, decimal? revisedAmount, string note, CancellationToken ct = default);
    Task ReviewByAccountantAsync(int exposureId, int accountantId, string note, CancellationToken ct = default);
    Task ConfirmDirectorPositionAsync(int exposureId, int directorId, string position, CancellationToken ct = default);
    Task ResolveAsync(int exposureId, int attorneyId, decimal settledAmount, string note, CancellationToken ct = default);
}

public sealed class AdverseCostExposureService(ApplicationDbContext db, INotificationService notifications) : IAdverseCostExposureService
{
    public async Task<AdverseCostExposure> LogAsync(AdverseCostExposure input, int attorneyId, CancellationToken ct = default)
    {
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == input.CaseId && x.LawyerId == attorneyId && x.Status == CaseStatus.Active, ct) ?? throw new UnauthorizedAccessException();
        var order = await db.Documents.SingleOrDefaultAsync(x => x.Id == input.CourtOrderDocumentId && x.CaseId == matter.Id && x.Category == DocumentCategory.CourtOrders, ct)
            ?? throw new InvalidOperationException("Select an uploaded court-order document belonging to this matter.");
        if (string.IsNullOrWhiteSpace(input.IssuingCourt) || input.RulingDateUtc > DateTime.UtcNow || input.CostOrderAmount <= 0) throw new InvalidOperationException("Enter the issuing court, a past ruling date and a positive cost-order amount.");
        var costs = await ExistingCostsAsync(matter.Id, ct);
        var available = await AvailableTrustRetainerAsync(matter.Id, ct);
        input.LoggedByAttorneyId = attorneyId; input.IssuingCourt = input.IssuingCourt.Trim(); input.CourtOrderDocument = order;
        input.ExistingCaseCostsSnapshot = costs; input.TotalExposure = input.CostOrderAmount + costs; input.AvailableTrustRetainerSnapshot = available; input.Shortfall = Math.Max(0m, input.TotalExposure - available);
        db.AdverseCostExposures.Add(input); await db.SaveChangesAsync(ct);
        db.AuditEntries.Add(new AuditEntry { ActorUserId = attorneyId, EntityType = nameof(AdverseCostExposure), EntityId = input.Id.ToString(), Action = "Adverse cost order logged", SafeMetadataJson = JsonSerializer.Serialize(new { input.CostOrderAmount, input.TotalExposure, input.Shortfall, input.CourtOrderDocumentId }) });
        foreach (var user in await db.Users.Where(x => x.IsActive && (x.Role == UserRole.Accountant || x.Role == UserRole.Director || x.Role == UserRole.Admin)).Select(x => new { x.Id, x.Role }).ToListAsync(ct))
            await notifications.QueueAsync(user.Id, "AdverseCostExposure", $"Adverse cost exposure: {matter.CaseNumber}", $"Exposure R {input.TotalExposure:N2}; shortfall R {input.Shortfall:N2}.", $"/AdverseCostExposure/Details/{input.Id}", $"adverse-exposure-{input.Id}-{user.Id}", ct);
        await db.SaveChangesAsync(ct); return input;
    }

    public async Task UpdateProgressAsync(int exposureId, int attorneyId, AdverseCostExposureStatus status, decimal? revisedAmount, string note, CancellationToken ct = default)
    {
        var item = await db.AdverseCostExposures.SingleOrDefaultAsync(x => x.Id == exposureId && x.LoggedByAttorneyId == attorneyId && x.ResolvedAtUtc == null, ct) ?? throw new UnauthorizedAccessException();
        if (status is not (AdverseCostExposureStatus.Negotiation or AdverseCostExposureStatus.Taxation) || string.IsNullOrWhiteSpace(note)) throw new InvalidOperationException("Select negotiation or taxation and record the current position.");
        if (revisedAmount is <= 0) throw new InvalidOperationException("Record a positive revised amount.");
        item.Status = status; item.RevisedAmount = revisedAmount; item.ProgressNote = note.Trim();
        db.AuditEntries.Add(new AuditEntry { ActorUserId = attorneyId, EntityType = nameof(AdverseCostExposure), EntityId = item.Id.ToString(), Action = $"Exposure progress: {status}", SafeMetadataJson = JsonSerializer.Serialize(new { revisedAmount, note = item.ProgressNote }) });
        await db.SaveChangesAsync(ct);
    }

    public async Task ReviewByAccountantAsync(int exposureId, int accountantId, string note, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(x => x.Id == accountantId && x.IsActive && x.Role == UserRole.Accountant, ct)) throw new UnauthorizedAccessException();
        var item = await db.AdverseCostExposures.FindAsync([exposureId], ct) ?? throw new KeyNotFoundException();
        if (string.IsNullOrWhiteSpace(note)) throw new InvalidOperationException("Record the accountant's exposure and shortfall review.");
        item.AccountantReviewedByUserId = accountantId; item.AccountantReviewedAtUtc = DateTime.UtcNow; item.ProgressNote = note.Trim();
        db.AuditEntries.Add(new AuditEntry { ActorUserId = accountantId, EntityType = nameof(AdverseCostExposure), EntityId = item.Id.ToString(), Action = "Accountant reviewed exposure", SafeMetadataJson = JsonSerializer.Serialize(new { item.Shortfall, note = item.ProgressNote }) }); await db.SaveChangesAsync(ct);
    }

    public async Task ConfirmDirectorPositionAsync(int exposureId, int directorId, string position, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(x => x.Id == directorId && x.IsActive && (x.Role == UserRole.Director || x.Role == UserRole.Admin), ct)) throw new UnauthorizedAccessException();
        var item = await db.AdverseCostExposures.FindAsync([exposureId], ct) ?? throw new KeyNotFoundException();
        if (string.IsNullOrWhiteSpace(position)) throw new InvalidOperationException("The Director must record the firm's position.");
        item.DirectorReviewedByUserId = directorId; item.DirectorReviewedAtUtc = DateTime.UtcNow; item.DirectorPosition = position.Trim(); if (item.Status == AdverseCostExposureStatus.Open) item.Status = AdverseCostExposureStatus.DirectorReviewed;
        db.AuditEntries.Add(new AuditEntry { ActorUserId = directorId, EntityType = nameof(AdverseCostExposure), EntityId = item.Id.ToString(), Action = "Director position confirmed", SafeMetadataJson = JsonSerializer.Serialize(new { item.DirectorPosition }) }); await db.SaveChangesAsync(ct);
    }

    public async Task ResolveAsync(int exposureId, int attorneyId, decimal settledAmount, string note, CancellationToken ct = default)
    {
        var item = await db.AdverseCostExposures.SingleOrDefaultAsync(x => x.Id == exposureId && x.LoggedByAttorneyId == attorneyId && x.ResolvedAtUtc == null, ct) ?? throw new UnauthorizedAccessException();
        if (settledAmount < 0 || string.IsNullOrWhiteSpace(note)) throw new InvalidOperationException("Record the final settled amount and resolution note.");
        item.FinalSettledAmount = settledAmount; item.Status = AdverseCostExposureStatus.Resolved; item.ResolvedAtUtc = DateTime.UtcNow; item.ProgressNote = note.Trim();
        db.AuditEntries.Add(new AuditEntry { ActorUserId = attorneyId, EntityType = nameof(AdverseCostExposure), EntityId = item.Id.ToString(), Action = "Adverse cost exposure resolved", SafeMetadataJson = JsonSerializer.Serialize(new { settledAmount, note = item.ProgressNote }) }); await db.SaveChangesAsync(ct);
    }

    private async Task<decimal> ExistingCostsAsync(int caseId, CancellationToken ct) => await db.TimeEntries.Where(x => x.CaseId == caseId).SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0m;
    private async Task<decimal> AvailableTrustRetainerAsync(int caseId, CancellationToken ct)
    {
        var clientId = await db.Cases.Where(x => x.Id == caseId).Select(x => x.ClientId).SingleAsync(ct);
        var retainer = await db.Retainers.Where(x => x.CaseId == caseId && x.Status == RetainerStatus.Active).SumAsync(x => (decimal?)x.AvailableBalance, ct) ?? 0m;
        var trust = await db.TrustAccounts.Where(x => x.ClientId == clientId && !x.IsFrozen && !x.IsClosed).Select(x => (decimal?)x.Balance).SingleOrDefaultAsync(ct) ?? 0m;
        return Math.Max(retainer, trust);
    }
}
