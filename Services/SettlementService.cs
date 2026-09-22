using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.DTOs.Settlement;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;

namespace SimplexLawFirm.Services
{
    public class SettlementService : ISettlementService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;

        // Estimated cost of negotiating / finalising a settlement (as % of matter value)
        private const decimal SettlementCostFactor = 0.05m;

        public SettlementService(
            ApplicationDbContext db,
            IAuditService audit,
            INotificationService notifications)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
        }

        // ─────────────────────────────────────────────
        //  UC-06  Steps 1-6 : Evaluate a single case
        // ─────────────────────────────────────────────
        public async Task<SettlementRecommendationDetailDto> EvaluateCaseAsync(
            int caseId, SettlementTriggerType triggerType)
        {
            // ── Step 1: Retrieve the active matter ──
            var matter = await _db.Cases
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.Id == caseId && c.Status == CaseStatus.Active)
                ?? throw new InvalidOperationException($"Active case {caseId} not found.");

            // ── Step 2a: Get projected litigation costs ──
            //   Prefer the latest LitigationStrategyDecision; fall back to MatterCostEstimate
            decimal projectedCost = await GetProjectedLitigationCostAsync(caseId, matter.MatterValue);

            // ── Step 2b: Get likelihood of success from the latest locked CaseForecast ──
            var forecast = await _db.CaseForecasts
                .Where(f => f.CaseId == caseId && f.Status == ForecastStatus.Locked)
                .OrderByDescending(f => f.LockedAtUtc)
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException(
                    $"No locked forecast exists for case {caseId}. A forecast is required before settlement evaluation.");

            decimal likelihoodOfSuccess = forecast.Probability ?? 0m;

            // ── Step 2c: Matter value ──
            decimal matterValue = matter.MatterValue;
            if (matterValue <= 0)
                throw new InvalidOperationException("Matter value must be greater than zero for settlement evaluation.");

            // ── Step 3: Calculate expected value of continuing litigation ──
            //   EV(litigate) = (MatterValue × Probability) − ProjectedCost
            decimal evLitigation = (matterValue * likelihoodOfSuccess) - projectedCost;

            // ── Step 2d: Historical settlement outcomes for comparable matters ──
            var (avgSettlement, comparableCount, comparablesJson) =
                await GetComparableSettlementsAsync(matter.CaseType, matterValue);

            // ── Step 3 cont: Expected value of settling ──
            //   EV(settle) = AvgComparableSettlement − (MatterValue × SettlementCostFactor)
            decimal estimatedSettlementCosts = matterValue * SettlementCostFactor;
            decimal evSettlement = avgSettlement - estimatedSettlementCosts;

            // ── Step 4: Flag if settlement looks better ──
            bool recommended = evSettlement > evLitigation && comparableCount >= 2;

            // ── Step 5: Generate rationale ──
            string rationale = BuildRationale(
                matterValue, projectedCost, likelihoodOfSuccess,
                evLitigation, avgSettlement, evSettlement,
                comparableCount, recommended);

            // Suggested settlement range: ±15% around average comparable
            decimal? sugLow = recommended ? avgSettlement * 0.85m : null;
            decimal? sugHigh = recommended ? avgSettlement * 1.15m : null;

            // Supersede any previous active recommendation for this case
            var previousActive = await _db.Set<SettlementRecommendation>()
                .Where(r => r.CaseId == caseId
                    && r.Status != SettlementRecommendationStatus.Superseded
                    && r.Status != SettlementRecommendationStatus.AttorneyRejected
                    && r.Status != SettlementRecommendationStatus.DirectorRejected)
                .ToListAsync();

            foreach (var prev in previousActive)
            {
                prev.Status = SettlementRecommendationStatus.Superseded;
                prev.SupersededAtUtc = DateTime.UtcNow;
            }

            // Create the new recommendation
            var recommendation = new SettlementRecommendation
            {
                CaseId = caseId,
                TriggerType = triggerType,
                ProjectedLitigationCost = projectedCost,
                LikelihoodOfSuccess = likelihoodOfSuccess,
                MatterValueSnapshot = matterValue,
                ExpectedLitigationValue = evLitigation,
                AverageComparableSettlement = avgSettlement,
                ExpectedSettlementValue = evSettlement,
                ComparableMatterCount = comparableCount,
                ComparableMattersJson = comparablesJson,
                SettlementRecommended = recommended,
                Rationale = rationale,
                SuggestedSettlementLow = sugLow,
                SuggestedSettlementHigh = sugHigh,
                AttorneyId = matter.LawyerId,
                Status = SettlementRecommendationStatus.PendingAttorneyReview,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Set<SettlementRecommendation>().Add(recommendation);
            await _db.SaveChangesAsync();

            // ── Step 6: Notify the responsible attorney ──
            if (matter.LawyerId.HasValue)
            {
                await _notifications.SendAsync(matter.LawyerId.Value,
                    recommended ? "Settlement Recommended" : "Settlement Evaluation Complete",
                    $"Case {matter.CaseNumber}: {rationale.Substring(0, Math.Min(200, rationale.Length))}...");
            }

            await _audit.LogAsync("SettlementRecommendation", recommendation.Id.ToString(),
                "Generated",
                $"Trigger: {triggerType}. EV(Litigate): R{evLitigation:N2}. EV(Settle): R{evSettlement:N2}. " +
                $"Recommended: {recommended}. Comparables: {comparableCount}.",
                actorUserId: null);

            return await MapToDetailDtoAsync(recommendation.Id);
        }

        // ─────────────────────────────────────────────
        //  UC-06  Step 1 (scheduled) : Batch evaluation
        // ─────────────────────────────────────────────
        public async Task<List<SettlementRecommendationDetailDto>> RunPeriodicEvaluationAsync()
        {
            // Get all active litigation cases that have a locked forecast and a matter value > 0
            var activeCaseIds = await _db.Cases
                .Where(c => c.Status == CaseStatus.Active && c.MatterValue > 0)
                .Select(c => c.Id)
                .ToListAsync();

            var casesWithForecasts = await _db.CaseForecasts
                .Where(f => activeCaseIds.Contains(f.CaseId)
                    && f.Status == ForecastStatus.Locked)
                .Select(f => f.CaseId)
                .Distinct()
                .ToListAsync();

            var results = new List<SettlementRecommendationDetailDto>();
            foreach (var caseId in casesWithForecasts)
            {
                try
                {
                    var result = await EvaluateCaseAsync(caseId, SettlementTriggerType.PeriodicEvaluation);
                    results.Add(result);
                }
                catch (InvalidOperationException)
                {
                    // Skip cases that don't meet prerequisites
                }
            }

            return results;
        }

        // ─────────────────────────────────────────────
        //  UC-06  Steps 7-8 : Attorney responds
        // ─────────────────────────────────────────────
        public async Task<SettlementRecommendationDetailDto> ProcessAttorneyResponseAsync(
            int attorneyUserId, AttorneySettlementResponseDto dto)
        {
            var rec = await _db.Set<SettlementRecommendation>()
                .Include(r => r.Case)
                .FirstOrDefaultAsync(r => r.Id == dto.RecommendationId)
                ?? throw new InvalidOperationException("Recommendation not found.");

            if (rec.Status != SettlementRecommendationStatus.PendingAttorneyReview)
                throw new InvalidOperationException("This recommendation is not pending attorney review.");

            switch (dto.Action)
            {
                case AttorneySettlementAction.Accept:
                    rec.Status = SettlementRecommendationStatus.AttorneyAccepted;
                    rec.AttorneyResponse = dto.Response ?? "Accepted.";
                    rec.AttorneyRespondedAtUtc = DateTime.UtcNow;

                    // ── Step 10: Update matter status ──
                    if (rec.Case != null)
                    {
                        rec.Case.Status = CaseStatus.Pending;
                        rec.Case.UpdatedAt = DateTime.UtcNow;
                    }
                    rec.Status = SettlementRecommendationStatus.SettlementInProgress;
                    break;

                case AttorneySettlementAction.Reject:
                    if (string.IsNullOrWhiteSpace(dto.Response))
                        throw new InvalidOperationException("A reason is required when rejecting a settlement recommendation.");

                    rec.Status = SettlementRecommendationStatus.AttorneyRejected;
                    rec.AttorneyResponse = dto.Response;
                    rec.AttorneyRespondedAtUtc = DateTime.UtcNow;
                    break;

                case AttorneySettlementAction.Escalate:
                    if (string.IsNullOrWhiteSpace(dto.Response))
                        throw new InvalidOperationException("Provide context when escalating to the Director.");

                    rec.Status = SettlementRecommendationStatus.EscalatedToDirector;
                    rec.AttorneyResponse = dto.Response;
                    rec.AttorneyRespondedAtUtc = DateTime.UtcNow;

                    // Notify Directors
                    var directors = await _db.Users
                        .Where(u => u.Role == UserRole.Director && u.IsActive)
                        .Select(u => u.Id)
                        .ToListAsync();

                    foreach (var dirId in directors)
                    {
                        await _notifications.SendAsync(dirId,
                            "Settlement Recommendation Escalated",
                            $"Case {rec.Case?.CaseNumber}: Attorney escalated settlement recommendation. " +
                            $"Reason: {dto.Response.Substring(0, Math.Min(200, dto.Response.Length))}");
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dto.Action));
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync("SettlementRecommendation", rec.Id.ToString(),
                $"Attorney_{dto.Action}",
                $"Attorney responded to settlement recommendation on case {rec.Case?.CaseNumber}. " +
                $"Action: {dto.Action}. Response: {dto.Response ?? "N/A"}",
                actorUserId: attorneyUserId);

            return await MapToDetailDtoAsync(rec.Id);
        }

        // ─────────────────────────────────────────────
        //  UC-06  Step 9 : Director decides
        // ─────────────────────────────────────────────
        public async Task<SettlementRecommendationDetailDto> ProcessDirectorDecisionAsync(
            int directorUserId, DirectorSettlementDecisionDto dto)
        {
            var rec = await _db.Set<SettlementRecommendation>()
                .Include(r => r.Case)
                .FirstOrDefaultAsync(r => r.Id == dto.RecommendationId)
                ?? throw new InvalidOperationException("Recommendation not found.");

            if (rec.Status != SettlementRecommendationStatus.EscalatedToDirector)
                throw new InvalidOperationException("This recommendation has not been escalated to the Director.");

            rec.DirectorId = directorUserId;
            rec.DirectorResponse = dto.Response;
            rec.DirectorDecidedAtUtc = DateTime.UtcNow;

            if (dto.Approve)
            {
                rec.Status = SettlementRecommendationStatus.DirectorApproved;

                // ── Step 10: Update matter status ──
                if (rec.Case != null)
                {
                    rec.Case.Status = CaseStatus.Pending;
                    rec.Case.UpdatedAt = DateTime.UtcNow;
                }
                rec.Status = SettlementRecommendationStatus.SettlementInProgress;

                // Notify attorney
                if (rec.AttorneyId.HasValue)
                {
                    await _notifications.SendAsync(rec.AttorneyId.Value,
                        "Settlement Approved by Director",
                        $"Case {rec.Case?.CaseNumber}: Director approved pursuing settlement. " +
                        $"Suggested range: R{rec.SuggestedSettlementLow:N2} – R{rec.SuggestedSettlementHigh:N2}.");
                }
            }
            else
            {
                rec.Status = SettlementRecommendationStatus.DirectorRejected;

                if (rec.AttorneyId.HasValue)
                {
                    await _notifications.SendAsync(rec.AttorneyId.Value,
                        "Settlement Rejected by Director",
                        $"Case {rec.Case?.CaseNumber}: Director rejected settlement. " +
                        $"Reason: {dto.Response.Substring(0, Math.Min(200, dto.Response.Length))}. " +
                        $"Continue litigation.");
                }
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync("SettlementRecommendation", rec.Id.ToString(),
                dto.Approve ? "DirectorApproved" : "DirectorRejected",
                $"Director decision on case {rec.Case?.CaseNumber}: {(dto.Approve ? "Approved" : "Rejected")}. " +
                $"Reason: {dto.Response}",
                actorUserId: directorUserId);

            return await MapToDetailDtoAsync(rec.Id);
        }

        // ─────────────────────────────────────────────
        //  Query methods
        // ─────────────────────────────────────────────
        public async Task<SettlementRecommendationDetailDto?> GetByIdAsync(int id)
        {
            var exists = await _db.Set<SettlementRecommendation>().AnyAsync(r => r.Id == id);
            return exists ? await MapToDetailDtoAsync(id) : null;
        }

        public async Task<List<SettlementRecommendationDetailDto>> GetPendingForAttorneyAsync(int attorneyUserId)
        {
            var ids = await _db.Set<SettlementRecommendation>()
                .Where(r => r.AttorneyId == attorneyUserId
                    && r.Status == SettlementRecommendationStatus.PendingAttorneyReview)
                .OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => r.Id).ToListAsync();

            var results = new List<SettlementRecommendationDetailDto>();
            foreach (var id in ids) results.Add(await MapToDetailDtoAsync(id));
            return results;
        }

        public async Task<List<SettlementRecommendationDetailDto>> GetPendingForDirectorAsync()
        {
            var ids = await _db.Set<SettlementRecommendation>()
                .Where(r => r.Status == SettlementRecommendationStatus.EscalatedToDirector)
                .OrderBy(r => r.CreatedAtUtc)
                .Select(r => r.Id).ToListAsync();

            var results = new List<SettlementRecommendationDetailDto>();
            foreach (var id in ids) results.Add(await MapToDetailDtoAsync(id));
            return results;
        }

        // ═════════════════════════════════════════════
        //  Private helpers
        // ═════════════════════════════════════════════

        private async Task<decimal> GetProjectedLitigationCostAsync(int caseId, decimal matterValue)
        {
            // Try LitigationStrategyDecision first (has ProjectedCostToCompletion)
            var strategy = await _db.LitigationStrategyDecisions
                .Where(d => d.CaseId == caseId && d.SupersededAtUtc == null)
                .OrderByDescending(d => d.RecordedAtUtc)
                .FirstOrDefaultAsync();

            if (strategy != null)
                return strategy.ProjectedCostToCompletion;

            // Fall back to the midpoint of the latest MatterCostEstimate
            var estimate = await _db.MatterCostEstimates
                .Where(e => e.LinkedCaseId == caseId)
                .OrderByDescending(e => e.LockedAtUtc)
                .FirstOrDefaultAsync();

            if (estimate != null)
                return (estimate.TotalLow + estimate.TotalHigh) / 2m;

            // Last resort: use 20% of matter value as a rough estimate
            return matterValue * 0.20m;
        }

        private async Task<(decimal avgSettlement, int count, string json)>
            GetComparableSettlementsAsync(string caseType, decimal matterValue)
        {
            // Find settled cases of the same type within ±50% of the matter value
            decimal low = matterValue * 0.50m;
            decimal high = matterValue * 1.50m;

            var comparables = await _db.Cases
                .Where(c => c.CaseType == caseType
                    && c.SettlementAmount.HasValue
                    && c.SettlementAmount.Value > 0
                    && c.MatterValue >= low
                    && c.MatterValue <= high
                    && c.RecordedOutcome != null)
                .Select(c => new
                {
                    c.Id,
                    c.CaseNumber,
                    c.MatterValue,
                    SettlementAmount = c.SettlementAmount!.Value,
                    c.CaseType
                })
                .OrderByDescending(c => c.SettlementAmount)
                .Take(20)
                .ToListAsync();

            if (!comparables.Any())
                return (0m, 0, "[]");

            decimal avg = comparables.Average(c => c.SettlementAmount);
            string json = System.Text.Json.JsonSerializer.Serialize(
                comparables.Select(c => new
                {
                    c.CaseNumber,
                    c.MatterValue,
                    c.SettlementAmount,
                    c.CaseType
                }));

            return (avg, comparables.Count, json);
        }

        private static string BuildRationale(
            decimal matterValue, decimal projectedCost, decimal likelihood,
            decimal evLitigation, decimal avgSettlement, decimal evSettlement,
            int comparableCount, bool recommended)
        {
            var lines = new List<string>
            {
                $"Matter value: R{matterValue:N2}.",
                $"Projected litigation cost to completion: R{projectedCost:N2}.",
                $"Likelihood of success: {likelihood * 100:N1}%.",
                $"Expected value of continuing litigation: R{evLitigation:N2} (MatterValue × Probability − Cost).",
                $"Average settlement in {comparableCount} comparable matter(s): R{avgSettlement:N2}.",
                $"Expected value of settling (net of estimated settlement costs): R{evSettlement:N2}."
            };

            if (comparableCount < 2)
            {
                lines.Add("CAUTION: Fewer than 2 comparable settled matters available. Data is insufficient for a confident recommendation.");
            }

            if (recommended)
            {
                decimal advantage = evSettlement - evLitigation;
                lines.Add($"RECOMMENDATION: Pursue settlement. Settlement yields an estimated R{advantage:N2} advantage over continued litigation.");
            }
            else
            {
                lines.Add("ASSESSMENT: Continuing litigation currently appears to offer a better expected outcome than settlement.");
            }

            return string.Join(" ", lines);
        }

        private async Task<SettlementRecommendationDetailDto> MapToDetailDtoAsync(int id)
        {
            var r = await _db.Set<SettlementRecommendation>()
                .Include(x => x.Case)
                .Include(x => x.Attorney)
                .Include(x => x.Director)
                .FirstAsync(x => x.Id == id);

            return new SettlementRecommendationDetailDto
            {
                Id = r.Id,
                CaseId = r.CaseId,
                CaseNumber = r.Case?.CaseNumber ?? "",
                CaseTitle = r.Case?.Title ?? "",
                CaseType = r.Case?.CaseType ?? "",
                TriggerType = r.TriggerType.ToString(),
                ProjectedLitigationCost = r.ProjectedLitigationCost,
                LikelihoodOfSuccess = r.LikelihoodOfSuccess,
                MatterValue = r.MatterValueSnapshot,
                ExpectedLitigationValue = r.ExpectedLitigationValue,
                AverageComparableSettlement = r.AverageComparableSettlement,
                ExpectedSettlementValue = r.ExpectedSettlementValue,
                ComparableMatterCount = r.ComparableMatterCount,
                SettlementRecommended = r.SettlementRecommended,
                Rationale = r.Rationale,
                SuggestedSettlementLow = r.SuggestedSettlementLow,
                SuggestedSettlementHigh = r.SuggestedSettlementHigh,
                AttorneyName = r.Attorney?.FullName,
                AttorneyId = r.AttorneyId,
                AttorneyResponse = r.AttorneyResponse,
                AttorneyRespondedAtUtc = r.AttorneyRespondedAtUtc,
                DirectorName = r.Director?.FullName,
                DirectorResponse = r.DirectorResponse,
                DirectorDecidedAtUtc = r.DirectorDecidedAtUtc,
                Status = r.Status.ToString(),
                CreatedAtUtc = r.CreatedAtUtc
            };
        }
    }
}
