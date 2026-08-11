using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.ViewModels;

namespace SimplexLawFirm.Services;

public interface ICaseGovernanceService
{
    Task<IReadOnlyList<StrategyOptionViewModel>> OptionsAsync(int caseId, int attorneyId, CancellationToken ct = default);
    Task<LitigationStrategyDecision> SelectStrategyAsync(int attorneyId, SelectLitigationStrategyViewModel input, CancellationToken ct = default);
    Task DecideStrategyAsync(int directorId, int decisionId, bool authorise, string reason, CancellationToken ct = default);
    Task<bool> RetestStrategyAsync(int caseId, CancellationToken ct = default);
    Task<CaseReadinessReportViewModel> ReviewReadinessAsync(int caseId, int userId, CancellationToken ct = default);
    Task<IReadOnlyList<ReadinessDashboardRowViewModel>> ReadinessDashboardAsync(int? attorneyId, CancellationToken ct = default);
    Task<IReadOnlyList<CaseReadinessReview>> ReadinessHistoryAsync(int caseId, int userId, CancellationToken ct = default);
    Task<CaseDocumentWaiver> RequestWaiverAsync(int attorneyId, int caseId, int requirementId, string reason, CancellationToken ct = default);
    Task DecideWaiverAsync(int directorId, int waiverId, bool approve, string reason, DateTime? deferDeadlineUtc = null, CancellationToken ct = default);
    Task MarkCourtReadyAsync(int attorneyId, int caseId, CancellationToken ct = default);
    Task<int> RunGovernanceAsync(DateTime nowUtc, CancellationToken ct = default);
}

public sealed class CaseGovernanceService(ApplicationDbContext db, INotificationService notifications) : ICaseGovernanceService
{
    private const decimal CostValueGate = .80m;
    private const decimal PoorProspectsGate = .40m;

    public async Task<IReadOnlyList<StrategyOptionViewModel>> OptionsAsync(int caseId, int attorneyId, CancellationToken ct = default)
    {
        var matter = await AssignedMatterAsync(caseId, attorneyId, ct);
        var incurred = await CostsAsync(caseId, ct);
        var comparable = await db.Cases.Where(x => x.Id != caseId && x.CaseType == matter.CaseType && x.Status == CaseStatus.Closed).ToListAsync(ct);
        var settlements = comparable.Where(x => x.SettlementAmount > 0).Select(x => x.SettlementAmount!.Value).OrderBy(x => x).ToList();
        decimal? low = settlements.Count == 0 ? null : settlements.First(), high = settlements.Count == 0 ? null : settlements.Last();
        return Enum.GetValues<LitigationStrategyType>().Select(strategy =>
        {
            var (factor, days) = StrategyProfile(strategy);
            return new StrategyOptionViewModel(strategy, decimal.Round(incurred + Math.Max(2_500, matter.MatterValue * factor), 2), days, comparable.Count, low, high);
        }).ToList();
    }

    public async Task<LitigationStrategyDecision> SelectStrategyAsync(int attorneyId, SelectLitigationStrategyViewModel input, CancellationToken ct = default)
    {
        var matter = await AssignedMatterAsync(input.CaseId, attorneyId, ct);
        if (matter.Status is CaseStatus.Closed or CaseStatus.Archived) throw new InvalidOperationException("A strategy can only be selected for an open matter.");
        if (matter.MatterValue <= 0 || string.IsNullOrWhiteSpace(matter.CaseType)) throw new InvalidOperationException("Record the matter value and case type before selecting a strategy.");
        if (string.IsNullOrWhiteSpace(input.Reasoning) || input.Reasoning.Trim().Length < 20) throw new ArgumentException("Record meaningful strategy reasoning.");
        var option = (await OptionsAsync(input.CaseId, attorneyId, ct)).Single(x => x.Strategy == input.Strategy);
        var prospects = await CurrentProspectsAsync(input.CaseId, matter.EvidenceStrength, ct);
        var costGate = option.ProjectedCost >= matter.MatterValue * CostValueGate;
        var prospectsGate = input.Strategy == LitigationStrategyType.Trial && prospects < PoorProspectsGate;
        if (prospectsGate && string.IsNullOrWhiteSpace(input.LowProspectsJustification)) throw new ArgumentException("Written justification is required to pursue trial on poor prospects.");
        var current = await db.LitigationStrategyDecisions.Where(x => x.CaseId == input.CaseId && x.Status != StrategyDecisionStatus.Superseded).ToListAsync(ct);
        current.ForEach(x => { x.Status = StrategyDecisionStatus.Superseded; x.SupersededAtUtc = DateTime.UtcNow; });
        var decision = new LitigationStrategyDecision
        {
            CaseId = input.CaseId, AttorneyId = attorneyId, Strategy = input.Strategy, Reasoning = input.Reasoning.Trim(),
            LowProspectsJustification = input.LowProspectsJustification?.Trim(), CostsIncurredSnapshot = await CostsAsync(input.CaseId, ct),
            ProjectedCostToCompletion = option.ProjectedCost, MatterValueSnapshot = matter.MatterValue, ProspectsSnapshot = prospects,
            ComparableMatterCount = option.ComparableCount, ComparableSettlementLow = option.SettlementLow, ComparableSettlementHigh = option.SettlementHigh,
            ExpectedDurationDays = option.ExpectedDurationDays, CostAuthorisationRequired = costGate, ProspectsAuthorisationRequired = prospectsGate,
            Status = costGate || prospectsGate ? StrategyDecisionStatus.PendingDirectorAuthorisation : StrategyDecisionStatus.Authorised,
            ReviewDueAtUtc = DateTime.UtcNow.AddDays(30)
        };
        matter.StrategyReviewRequired = false; matter.IsCourtReady = false; matter.CourtReadyAtUtc = null;
        db.LitigationStrategyDecisions.Add(decision);
        db.AuditEntries.Add(new() { ActorUserId = attorneyId, EntityType = nameof(LitigationStrategyDecision), EntityId = input.CaseId.ToString(), Action = $"Strategy selected: {input.Strategy}", SafeMetadataJson = JsonSerializer.Serialize(new { option.ProjectedCost, matter.MatterValue, prospects, costGate, prospectsGate }) });
        await db.SaveChangesAsync(ct);
        if (decision.Status == StrategyDecisionStatus.PendingDirectorAuthorisation)
            foreach (var director in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct))
                await notifications.QueueAsync(director, "StrategyAuthorisation", "Litigation strategy requires authorisation", $"{matter.CaseNumber}: {input.Strategy} requires a director decision.", $"/CaseGovernance/StrategyReview/{decision.Id}", $"strategy:{decision.Id}:{director}", ct);
        await db.SaveChangesAsync(ct); return decision;
    }

    public async Task DecideStrategyAsync(int directorId, int decisionId, bool authorise, string reason, CancellationToken ct = default)
    {
        await EnsureDirectorAsync(directorId, ct);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Director reasons are required.");
        var decision = await db.LitigationStrategyDecisions.SingleOrDefaultAsync(x => x.Id == decisionId && x.Status == StrategyDecisionStatus.PendingDirectorAuthorisation, ct) ?? throw new InvalidOperationException("Strategy is not awaiting authorisation.");
        decision.Status = authorise ? StrategyDecisionStatus.Authorised : StrategyDecisionStatus.Refused; decision.DirectorId = directorId; decision.DirectorReason = reason.Trim(); decision.DirectorDecidedAtUtc = DateTime.UtcNow;
        db.AuditEntries.Add(new() { ActorUserId = directorId, EntityType = nameof(LitigationStrategyDecision), EntityId = decision.Id.ToString(), Action = authorise ? "Strategy authorised" : "Strategy refused" });
        await notifications.QueueAsync(decision.AttorneyId, "StrategyDecision", authorise ? "Strategy authorised" : "Strategy refused", reason.Trim(), $"/CaseGovernance/Strategy/{decision.CaseId}", $"strategy-decision:{decision.Id}", ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> RetestStrategyAsync(int caseId, CancellationToken ct = default)
    {
        var decision = await db.LitigationStrategyDecisions.Where(x => x.CaseId == caseId && x.Status == StrategyDecisionStatus.Authorised).OrderByDescending(x => x.RecordedAtUtc).FirstOrDefaultAsync(ct);
        if (decision is null) return false;
        var matter = await db.Cases.FindAsync([caseId], ct) ?? throw new KeyNotFoundException();
        var costs = await CostsAsync(caseId, ct); var prospects = await CurrentProspectsAsync(caseId, matter.EvidenceStrength, ct);
        var materiallyChanged = costs > decision.CostsIncurredSnapshot * 1.15m || Math.Abs(prospects - decision.ProspectsSnapshot) >= .10m || decision.ReviewDueAtUtc <= DateTime.UtcNow;
        if (!materiallyChanged) return false;
        decision.Status = StrategyDecisionStatus.RequiresReview; matter.StrategyReviewRequired = true; matter.IsCourtReady = false; matter.CourtReadyAtUtc = null;
        db.AuditEntries.Add(new() { EntityType = nameof(LitigationStrategyDecision), EntityId = decision.Id.ToString(), Action = "Strategy reopened after material change" });
        await notifications.QueueAsync(decision.AttorneyId, "StrategyReview", "Litigation strategy must be reviewed", $"Costs or prospects changed materially for {matter.CaseNumber}.", $"/CaseGovernance/Strategy/{caseId}", $"strategy-retest:{decision.Id}:{costs}:{prospects}", ct);
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<CaseReadinessReportViewModel> ReviewReadinessAsync(int caseId, int userId, CancellationToken ct = default)
    {
        var matter = await db.Cases.Include(x => x.Documents).SingleOrDefaultAsync(x => x.Id == caseId, ct) ?? throw new KeyNotFoundException("Case not found.");
        if (!await MayManageAsync(matter, userId, ct)) throw new UnauthorizedAccessException();
        var requirements = await db.CaseDocumentRequirements.Where(x => x.IsActive && (x.CaseType == matter.CaseType || x.CaseType == "General")).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        var waivers = await db.CaseDocumentWaivers.Where(x => x.CaseId == caseId).OrderByDescending(x => x.RequestedAtUtc).ToListAsync(ct);
        var external = await db.ExternalEvidenceDocuments.Include(x => x.Request).Where(x => x.Request.CaseId == caseId).ToListAsync(ct);
        var items = requirements.Select(r => new DocumentReadinessItemViewModel(r, matter.Documents.Where(d => d.RequirementCode == r.Code).OrderByDescending(d => d.UploadedAt).FirstOrDefault(), external.Where(d => d.RequirementCode == r.Code).OrderByDescending(d => d.UploadedAtUtc).FirstOrDefault(), waivers.FirstOrDefault(w => w.RequirementId == r.Id))).ToList();
        var authorised = await db.LitigationStrategyDecisions.AnyAsync(x => x.CaseId == caseId && x.Status == StrategyDecisionStatus.Authorised, ct);
        var ready = authorised && items.All(x => x.Requirement.Importance != DocumentRequirementImportance.Mandatory || x.IsHeld || x.Waiver?.Status == DocumentWaiverStatus.Approved);
        db.CaseReadinessReviews.Add(new() { CaseId = caseId, ReviewedByUserId = userId, HeldCount = items.Count(x => x.IsHeld), MissingMandatoryCount = items.Count(x => x.Requirement.Importance == DocumentRequirementImportance.Mandatory && !x.IsHeld && x.Waiver?.Status != DocumentWaiverStatus.Approved), MissingAdvisoryCount = items.Count(x => x.Requirement.Importance == DocumentRequirementImportance.Advisory && !x.IsHeld), CourtReady = ready, SnapshotJson = JsonSerializer.Serialize(items.Select(x => new { x.Requirement.Code, x.Requirement.Importance, Held = x.IsHeld, Waiver = x.Waiver?.Status })) });
        await db.SaveChangesAsync(ct); return new(matter, items, authorised, ready);
    }

    /// <summary>
    /// Matter list for the readiness dashboard. Deliberately read-only: unlike
    /// <see cref="ReviewReadinessAsync"/> it must not write an audit review just because
    /// someone opened the dashboard, so mandatory-document status is computed inline rather
    /// than by calling into the reviewing method.
    /// </summary>
    public async Task<IReadOnlyList<ReadinessDashboardRowViewModel>> ReadinessDashboardAsync(int? attorneyId, CancellationToken ct = default)
    {
        var cases = await db.Cases.Include(x => x.Lawyer).Where(x => x.Status == CaseStatus.Active && (attorneyId == null || x.LawyerId == attorneyId)).ToListAsync(ct);
        if (cases.Count == 0) return [];
        var caseIds = cases.Select(x => x.Id).ToList();
        var requirementsByType = await db.CaseDocumentRequirements.Where(x => x.IsActive && x.Importance == DocumentRequirementImportance.Mandatory).ToListAsync(ct);
        var heldByCase = (await db.Documents.Where(x => x.CaseId != null && caseIds.Contains(x.CaseId.Value) && x.RequirementCode != null).Select(x => new { CaseId = x.CaseId!.Value, x.RequirementCode }).ToListAsync(ct))
            .Concat(await db.ExternalEvidenceDocuments.Include(x => x.Request).Where(x => caseIds.Contains(x.Request.CaseId) && x.RequirementCode != null).Select(x => new { x.Request.CaseId, x.RequirementCode }).ToListAsync(ct))
            .GroupBy(x => x.CaseId).ToDictionary(g => g.Key, g => g.Select(x => x.RequirementCode!).ToHashSet());
        var waivedByCase = (await db.CaseDocumentWaivers.Where(x => caseIds.Contains(x.CaseId) && x.Status == DocumentWaiverStatus.Approved).Select(x => new { x.CaseId, x.RequirementId }).ToListAsync(ct))
            .GroupBy(x => x.CaseId).ToDictionary(g => g.Key, g => g.Select(x => x.RequirementId).ToHashSet());
        var nextCourtDateByCase = (await db.CalendarEvents.Where(x => x.CaseId != null && caseIds.Contains(x.CaseId.Value) && x.Status != EventStatus.Cancelled && (x.Type == EventType.Hearing || x.Type == EventType.CourtAppearance) && x.StartDateTime >= DateTime.UtcNow).ToListAsync(ct))
            .GroupBy(x => x.CaseId!.Value).ToDictionary(g => g.Key, g => g.Min(x => x.StartDateTime));
        var escalatedCaseIds = (await db.AuditEntries.Where(x => x.EntityType == nameof(CaseReadinessReview) && caseIds.Select(id => id.ToString()).Contains(x.EntityId)).Select(x => x.EntityId).ToListAsync(ct)).ToHashSet();

        return cases.Select(matter =>
        {
            var requirements = requirementsByType.Where(r => r.CaseType == matter.CaseType || r.CaseType == "General");
            var held = heldByCase.GetValueOrDefault(matter.Id, []);
            var waived = waivedByCase.GetValueOrDefault(matter.Id, []);
            var missing = requirements.Count(r => !held.Contains(r.Code) && !waived.Contains(r.Id));
            var nextCourtDate = nextCourtDateByCase.TryGetValue(matter.Id, out var date) ? date : (DateTime?)null;
            return new ReadinessDashboardRowViewModel(matter, nextCourtDate, missing, escalatedCaseIds.Contains(matter.Id.ToString()));
        }).OrderBy(x => x.NextCourtDate ?? DateTime.MaxValue).ToList();
    }

    public async Task<IReadOnlyList<CaseReadinessReview>> ReadinessHistoryAsync(int caseId, int userId, CancellationToken ct = default)
    {
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == caseId, ct) ?? throw new KeyNotFoundException("Case not found.");
        if (!await MayManageAsync(matter, userId, ct)) throw new UnauthorizedAccessException();
        return await db.CaseReadinessReviews.Where(x => x.CaseId == caseId).OrderByDescending(x => x.ReviewedAtUtc).ToListAsync(ct);
    }

    public async Task<CaseDocumentWaiver> RequestWaiverAsync(int attorneyId, int caseId, int requirementId, string reason, CancellationToken ct = default)
    {
        await AssignedMatterAsync(caseId, attorneyId, ct); if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Explain why the mandatory document cannot be obtained.");
        var requirement = await db.CaseDocumentRequirements.SingleOrDefaultAsync(x => x.Id == requirementId && x.Importance == DocumentRequirementImportance.Mandatory, ct) ?? throw new ArgumentException("Mandatory requirement not found.");
        if (await db.CaseDocumentWaivers.AnyAsync(x => x.CaseId == caseId && x.RequirementId == requirementId && x.Status == DocumentWaiverStatus.PendingDirector, ct))
            throw new InvalidOperationException("A Director waiver request for this document is already pending.");
        var waiver = new CaseDocumentWaiver { CaseId = caseId, RequirementId = requirementId, RequestedByAttorneyId = attorneyId, Reason = reason.Trim(), Status = DocumentWaiverStatus.PendingDirector };
        db.Add(waiver); await db.SaveChangesAsync(ct);
        foreach (var director in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct)) await notifications.QueueAsync(director, "DocumentWaiver", "Mandatory document waiver requested", requirement.Name, $"/CaseGovernance/WaiverReview/{waiver.Id}", $"waiver:{waiver.Id}:{director}", ct);
        await db.SaveChangesAsync(ct); return waiver;
    }

    public async Task DecideWaiverAsync(int directorId, int waiverId, bool approve, string reason, DateTime? deferDeadlineUtc = null, CancellationToken ct = default)
    {
        await EnsureDirectorAsync(directorId, ct); if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Director reasons are required.");
        var waiver = await db.CaseDocumentWaivers.Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == waiverId && x.Status == DocumentWaiverStatus.PendingDirector, ct) ?? throw new InvalidOperationException("Waiver is not pending.");
        var deferring = !approve && deferDeadlineUtc.HasValue;
        if (deferring)
        {
            if (deferDeadlineUtc!.Value.Date < DateTime.UtcNow.Date) throw new ArgumentException("The deadline cannot be in the past.");
            if (waiver.Case.CourtReadyAtUtc is null)
            {
                var nextHearing = await db.CalendarEvents.Where(x => x.CaseId == waiver.CaseId && (x.Type == EventType.CourtAppearance || x.Type == EventType.Hearing) && x.StartDateTime >= DateTime.UtcNow).OrderBy(x => x.StartDateTime).Select(x => (DateTime?)x.StartDateTime).FirstOrDefaultAsync(ct);
                if (nextHearing.HasValue && deferDeadlineUtc.Value.Date > nextHearing.Value.Date) throw new ArgumentException("The deadline cannot be after the matter's next court date.");
            }
        }
        waiver.Status = approve ? DocumentWaiverStatus.Approved : deferring ? DocumentWaiverStatus.Deferred : DocumentWaiverStatus.Refused;
        waiver.DirectorId = directorId; waiver.DirectorReason = reason.Trim(); waiver.DecidedAtUtc = DateTime.UtcNow;
        waiver.DeadlineAtUtc = deferring ? deferDeadlineUtc : null;
        var action = approve ? "Mandatory document waiver approved" : deferring ? "Mandatory document waiver deferred — deadline set" : "Mandatory document waiver refused";
        db.AuditEntries.Add(new() { ActorUserId = directorId, EntityType = nameof(CaseDocumentWaiver), EntityId = waiver.Id.ToString(), Action = action });
        var message = deferring ? $"{reason.Trim()} Submit the document by {deferDeadlineUtc:dd MMM yyyy}." : reason.Trim();
        await notifications.QueueAsync(waiver.RequestedByAttorneyId, "DocumentWaiverDecision", approve ? "Document waiver approved" : deferring ? "Document deadline set — action required" : "Document waiver refused", message, $"/CaseGovernance/Readiness/{waiver.CaseId}", $"waiver-decision:{waiver.Id}", ct); await db.SaveChangesAsync(ct);
    }

    public async Task MarkCourtReadyAsync(int attorneyId, int caseId, CancellationToken ct = default)
    {
        await RetestStrategyAsync(caseId, ct); var report = await ReviewReadinessAsync(caseId, attorneyId, ct);
        if (!report.HasAuthorisedStrategy) throw new InvalidOperationException("An authorised litigation strategy is required before court-ready status.");
        if (report.MissingMandatory.Count != 0) throw new InvalidOperationException($"Mandatory documents outstanding: {string.Join(", ", report.MissingMandatory.Select(x => x.Requirement.Name))}.");
        report.Case.IsCourtReady = true; report.Case.CourtReadyAtUtc = DateTime.UtcNow;
        db.AuditEntries.Add(new() { ActorUserId = attorneyId, EntityType = nameof(Case), EntityId = caseId.ToString(), Action = "Matter marked court-ready" }); await db.SaveChangesAsync(ct);
    }

    public async Task<int> RunGovernanceAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var changes = 0;
        var activeCases = await db.LitigationStrategyDecisions.Where(x => x.Status == StrategyDecisionStatus.Authorised).Select(x => x.CaseId).Distinct().ToListAsync(ct);
        foreach (var caseId in activeCases) if (await RetestStrategyAsync(caseId, ct)) changes++;
        var approaching = await db.CalendarEvents.Include(x => x.Case).Where(x => x.CaseId != null && x.Status != EventStatus.Cancelled && (x.Type == EventType.Hearing || x.Type == EventType.CourtAppearance) && x.StartDateTime >= nowUtc && x.StartDateTime <= nowUtc.AddDays(14)).ToListAsync(ct);
        foreach (var courtEvent in approaching.Where(x => x.Case?.LawyerId != null))
        {
            var matter = courtEvent.Case!; var requirements = await db.CaseDocumentRequirements.Where(x => x.IsActive && x.Importance == DocumentRequirementImportance.Mandatory && (x.CaseType == matter.CaseType || x.CaseType == "General")).ToListAsync(ct);
            var held = await db.Documents.Where(x => x.CaseId == matter.Id && x.RequirementCode != null).Select(x => x.RequirementCode!).ToListAsync(ct);
            held.AddRange(await db.ExternalEvidenceDocuments.Where(x => x.Request.CaseId == matter.Id && x.RequirementCode != null).Select(x => x.RequirementCode!).ToListAsync(ct));
            var waived = await db.CaseDocumentWaivers.Where(x => x.CaseId == matter.Id && x.Status == DocumentWaiverStatus.Approved).Select(x => x.RequirementId).ToListAsync(ct);
            var missing = requirements.Where(x => !held.Contains(x.Code) && !waived.Contains(x.Id)).ToList(); if (missing.Count == 0) continue;
            var days = Math.Max(0, (int)Math.Ceiling((courtEvent.StartDateTime - nowUtc).TotalDays)); var names = string.Join(", ", missing.Select(x => x.Name));
            await notifications.QueueAsync(matter.LawyerId!.Value, "DocumentReadinessEscalation", $"{days}-day court-readiness warning", $"{matter.CaseNumber} is missing mandatory documents: {names}.", $"/CaseGovernance/Readiness?caseId={matter.Id}", $"readiness:{courtEvent.Id}:{days}:{matter.LawyerId}", ct);
            if (days <= 1) foreach (var director in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct)) await notifications.QueueAsync(director, "FinalDocumentReadinessEscalation", "Final reminder: mandatory case documents missing", $"{matter.CaseNumber} is approaching court with: {names} outstanding.", $"/CaseGovernance/Readiness?caseId={matter.Id}", $"readiness-final:{courtEvent.Id}:{director}", ct);
            db.AuditEntries.Add(new() { EntityType = nameof(CaseReadinessReview), EntityId = matter.Id.ToString(), Action = days <= 1 ? "Final document readiness escalation" : "Court-date document readiness escalation" }); changes++;
        }
        await db.SaveChangesAsync(ct); return changes;
    }

    private async Task<Case> AssignedMatterAsync(int caseId, int attorneyId, CancellationToken ct) => await db.Cases.SingleOrDefaultAsync(x => x.Id == caseId && x.LawyerId == attorneyId, ct) ?? throw new UnauthorizedAccessException("Only the assigned attorney may manage this decision.");
    private async Task<bool> MayManageAsync(Case matter, int userId, CancellationToken ct) => matter.LawyerId == userId || await db.Users.AnyAsync(x => x.Id == userId && x.Role == UserRole.Admin, ct);
    private async Task EnsureDirectorAsync(int userId, CancellationToken ct) { if (!await db.Users.AnyAsync(x => x.Id == userId && x.Role == UserRole.Admin && x.IsActive, ct)) throw new UnauthorizedAccessException("Director authority is required."); }
    private Task<decimal> CostsAsync(int caseId, CancellationToken ct) => db.TimeEntries.Where(x => x.CaseId == caseId).SumAsync(x => (decimal?)x.TotalAmount, ct).ContinueWith(x => x.Result ?? 0, ct);
    private async Task<decimal> CurrentProspectsAsync(int caseId, decimal fallback, CancellationToken ct) => await db.CaseForecasts.Where(x => x.CaseId == caseId && (x.Status == ForecastStatus.Locked || x.Status == ForecastStatus.Scored)).OrderByDescending(x => x.RequestedAtUtc).Select(x => x.Probability).FirstOrDefaultAsync(ct) ?? fallback;
    private static (decimal Factor, int Days) StrategyProfile(LitigationStrategyType strategy) => strategy switch { LitigationStrategyType.Settle => (.08m, 30), LitigationStrategyType.Negotiate => (.12m, 60), LitigationStrategyType.Mediate => (.18m, 90), LitigationStrategyType.Pursue => (.35m, 180), LitigationStrategyType.Trial => (.65m, 365), _ => (.03m, 14) };
}

public sealed class CaseGovernanceWorker(IServiceScopeFactory scopes, ILogger<CaseGovernanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<ICaseGovernanceService>().RunGovernanceAsync(DateTime.UtcNow, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Case governance cycle failed."); }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
