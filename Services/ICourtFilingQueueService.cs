using SimplexLawFirm.DTOs.CourtFiling;

namespace SimplexLawFirm.Services
{
    public interface ICourtFilingQueueService
    {
        Task<CourtFilingDetailDto> SubmitAsync(int attorneyId, SubmitCourtFilingDto dto);
        Task<CourtFilingDetailDto> ValidateAsync(int reviewerId, ValidateCourtFilingDto dto);
        Task<CourtFilingDetailDto> ReturnToAttorneyAsync(int reviewerId, ReturnCourtFilingDto dto);
        Task<CourtFilingDetailDto> MarkFiledAsync(int userId, int submissionId, string? courtReference);
        Task<CourtFilingDetailDto?> GetByIdAsync(int id);
        Task<List<CourtFilingDetailDto>> GetQueueAsync(int? caseId = null);
        Task<List<CourtFilingDetailDto>> GetForCaseAsync(int caseId);
    }
}
