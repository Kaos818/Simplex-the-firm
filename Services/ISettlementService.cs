using SimplexLawFirm.DTOs.Settlement;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Services
{
    public interface ISettlementService
    {
        /// <summary>Evaluate a single case for settlement opportunity (UC-06 steps 1-6).</summary>
        Task<SettlementRecommendationDetailDto> EvaluateCaseAsync(int caseId, SettlementTriggerType triggerType);

        /// <summary>Run periodic evaluation across all active litigation matters (UC-06 step 1 scheduled).</summary>
        Task<List<SettlementRecommendationDetailDto>> RunPeriodicEvaluationAsync();

        /// <summary>Attorney responds to a recommendation (UC-06 steps 7-8).</summary>
        Task<SettlementRecommendationDetailDto> ProcessAttorneyResponseAsync(int attorneyUserId, AttorneySettlementResponseDto dto);

        /// <summary>Director decides on an escalated recommendation (UC-06 step 9).</summary>
        Task<SettlementRecommendationDetailDto> ProcessDirectorDecisionAsync(int directorUserId, DirectorSettlementDecisionDto dto);

        /// <summary>Get a single recommendation by ID.</summary>
        Task<SettlementRecommendationDetailDto?> GetByIdAsync(int id);

        /// <summary>List active recommendations for a given attorney.</summary>
        Task<List<SettlementRecommendationDetailDto>> GetPendingForAttorneyAsync(int attorneyUserId);

        /// <summary>List recommendations escalated to the Director.</summary>
        Task<List<SettlementRecommendationDetailDto>> GetPendingForDirectorAsync();
    }
}
