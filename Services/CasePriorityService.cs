using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using MatterTaskStatus = SimplexLawFirm.Models.TaskStatus;

namespace SimplexLawFirm.Services;

public interface ICasePriorityService
{
    Task<IReadOnlyList<CasePriorityResult>> AnalyseAsync(int? assignedAttorneyId, CancellationToken ct = default);
    Task<CasePriorityWeightConfiguration> GetConfigurationAsync(CancellationToken ct = default);
    Task UpdateConfigurationAsync(CasePriorityWeightConfiguration input, int actorUserId, CancellationToken ct = default);
}

public sealed class CasePriorityService(ApplicationDbContext db) : ICasePriorityService
{
    public async Task<IReadOnlyList<CasePriorityResult>> AnalyseAsync(int? assignedAttorneyId, CancellationToken ct = default)
    {
        var configuration = await GetConfigurationAsync(ct);
        var matters = await db.Cases.Include(x => x.Client)
            .Where(x => x.Status == CaseStatus.Active && (assignedAttorneyId == null || x.LawyerId == assignedAttorneyId))
            .OrderBy(x => x.CaseNumber).ToListAsync(ct);
        var matterIds = matters.Select(x => x.Id).ToArray();
        var forecasts = await db.CaseForecasts.Where(x => matterIds.Contains(x.CaseId) && (x.Status == ForecastStatus.Locked || x.Status == ForecastStatus.Scored))
            .GroupBy(x => x.CaseId).Select(x => new { CaseId = x.Key, Probability = x.OrderByDescending(y => y.RequestedAtUtc).Select(y => y.Probability).FirstOrDefault() }).ToDictionaryAsync(x => x.CaseId, x => x.Probability, ct);
        var events = await db.CalendarEvents.Where(x => x.CaseId != null && matterIds.Contains(x.CaseId.Value) && x.Status != EventStatus.Cancelled)
            .Select(x => new PriorityEvent(x.CaseId!.Value, x.StartDateTime, x.Type)).ToListAsync(ct);
        var tasks = await db.Tasks.Where(x => x.CaseId != null && matterIds.Contains(x.CaseId.Value))
            .Select(x => new PriorityTask(x.CaseId!.Value, x.Status, x.DueDate)).ToListAsync(ct);
        var documents = await db.Documents.Where(x => x.CaseId != null && matterIds.Contains(x.CaseId.Value))
            .Select(x => new { CaseId = x.CaseId!.Value, x.RequirementCode }).ToListAsync(ct);
        var requirements = await db.CaseDocumentRequirements.Where(x => x.Importance == DocumentRequirementImportance.Mandatory).ToListAsync(ct);
        var outstandingInvoices = await db.Invoices.Where(x => x.CaseId != null && matterIds.Contains(x.CaseId.Value) && x.Status != InvoiceStatus.Paid && x.Status != InvoiceStatus.Cancelled)
            .GroupBy(x => x.CaseId!.Value).Select(x => new { CaseId = x.Key, Amount = x.Sum(y => y.TotalAmount) }).ToDictionaryAsync(x => x.CaseId, x => x.Amount, ct);
        var retainers = await db.Retainers.Where(x => x.CaseId != null && matterIds.Contains(x.CaseId.Value) && x.Status == RetainerStatus.Active)
            .GroupBy(x => x.CaseId!.Value).Select(x => new { CaseId = x.Key, Balance = x.Sum(y => y.AvailableBalance) }).ToDictionaryAsync(x => x.CaseId, x => x.Balance, ct);

        return matters.Select(matter => Analyse(matter, configuration, forecasts.GetValueOrDefault(matter.Id),
            events.Where(x => x.CaseId == matter.Id).ToList(), tasks.Where(x => x.CaseId == matter.Id).ToList(),
            documents.Where(x => x.CaseId == matter.Id).Select(x => x.RequirementCode).ToHashSet(), requirements,
            outstandingInvoices.GetValueOrDefault(matter.Id), retainers.GetValueOrDefault(matter.Id))).OrderByDescending(x => x.Score).ToList();
    }

    public async Task<CasePriorityWeightConfiguration> GetConfigurationAsync(CancellationToken ct = default)
    {
        var configuration = await db.CasePriorityWeightConfigurations.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (configuration is not null) return configuration;
        configuration = new CasePriorityWeightConfiguration { Id = 1 };
        db.CasePriorityWeightConfigurations.Add(configuration);
        await db.SaveChangesAsync(ct);
        return configuration;
    }

    public async Task UpdateConfigurationAsync(CasePriorityWeightConfiguration input, int actorUserId, CancellationToken ct = default)
    {
        var weights = new[] { input.RiskWeight, input.UrgencyWeight, input.ComplexityWeight, input.DocumentReadinessWeight, input.FinancialExposureWeight };
        if (weights.Any(x => x < 0) || weights.Sum() != 100m) throw new InvalidOperationException("Priority factor weights must be non-negative and total exactly 100%.");
        var configuration = await GetConfigurationAsync(ct);
        configuration.RiskWeight = input.RiskWeight; configuration.UrgencyWeight = input.UrgencyWeight;
        configuration.ComplexityWeight = input.ComplexityWeight; configuration.DocumentReadinessWeight = input.DocumentReadinessWeight;
        configuration.FinancialExposureWeight = input.FinancialExposureWeight; configuration.UpdatedAtUtc = DateTime.UtcNow; configuration.UpdatedByUserId = actorUserId;
        db.AuditEntries.Add(new AuditEntry { ActorUserId = actorUserId, EntityType = nameof(CasePriorityWeightConfiguration), EntityId = configuration.Id.ToString(), Action = "Priority weights updated", SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(weights) });
        await db.SaveChangesAsync(ct);
    }

    private static CasePriorityResult Analyse(Case matter, CasePriorityWeightConfiguration c, decimal? forecastProbability,
        List<PriorityEvent> events, List<PriorityTask> tasks, HashSet<string?> documentCodes, List<CaseDocumentRequirement> requirements, decimal outstanding, decimal retainerBalance)
    {
        var risk = Math.Round((1m - (forecastProbability ?? matter.EvidenceStrength)) * 100m, 1);
        var now = DateTime.UtcNow;
        var nextDeadline = events.Where(x => x.Type is EventType.Deadline or EventType.FilingDeadline or EventType.CourtAppearance or EventType.Hearing).Select(x => x.StartDateTime)
            .Concat(tasks.Where(x => x.Status != MatterTaskStatus.Completed && x.DueDate != null).Select(x => x.DueDate!.Value)).OrderBy(x => x).FirstOrDefault();
        var urgency = nextDeadline == default ? 15m : nextDeadline <= now ? 100m : nextDeadline <= now.AddDays(2) ? 90m : nextDeadline <= now.AddDays(7) ? 70m : nextDeadline <= now.AddDays(30) ? 40m : 15m;
        var outstandingTasks = tasks.Count(x => x.Status != MatterTaskStatus.Completed);
        var complexity = Math.Min(100m, 20m + outstandingTasks * 12m + (matter.CaseType is "Personal Injury" or "Commercial" ? 20m : 0m) + Math.Min(20m, documentCodes.Count * 3m));
        var applicable = requirements.Where(x => x.CaseType == "General" || x.CaseType == matter.CaseType).Select(x => x.Code).Distinct().ToList();
        var missing = applicable.Count(code => !documentCodes.Contains(code));
        var readiness = applicable.Count == 0 ? 0m : Math.Round(missing * 100m / applicable.Count, 1);
        var exposure = Math.Max(0m, outstanding - retainerBalance);
        var financial = matter.MatterValue > 0 ? Math.Min(100m, Math.Round(exposure * 100m / matter.MatterValue, 1)) : (exposure > 0 ? 80m : 0m);
        var factors = new List<CasePriorityFactor> {
            new("Risk", risk, c.RiskWeight, forecastProbability is null ? "Derived from recorded evidence strength; no committed prospects forecast is available." : $"Committed prospects forecast: {forecastProbability.Value:P0}."),
            new("Urgency", urgency, c.UrgencyWeight, nextDeadline == default ? "No open court or task deadline is recorded." : $"Nearest open deadline: {nextDeadline:dd MMM yyyy}."),
            new("Complexity", complexity, c.ComplexityWeight, $"{outstandingTasks} open task(s); matter type: {matter.CaseType}."),
            new("Document readiness", readiness, c.DocumentReadinessWeight, $"{missing} of {applicable.Count} mandatory document requirement(s) are not held."),
            new("Financial exposure", financial, c.FinancialExposureWeight, $"Outstanding R{outstanding:N2}; available retainer R{retainerBalance:N2}; uncovered R{exposure:N2}.") };
        var score = Math.Round(factors.Sum(x => x.Contribution), 1);
        var band = score >= 75 ? CasePriorityBand.Critical : score >= 50 ? CasePriorityBand.High : score >= 25 ? CasePriorityBand.Medium : CasePriorityBand.Low;
        return new CasePriorityResult(matter.Id, matter.CaseNumber, matter.Title, matter.Client?.FullName ?? "Unassigned client", band, score, factors);
    }

    private sealed record PriorityEvent(int CaseId, DateTime StartDateTime, EventType Type);
    private sealed record PriorityTask(int CaseId, MatterTaskStatus Status, DateTime? DueDate);
}
