using SimplexLawFirm.DTOs.Witnesses;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Services
{
    public interface IWitnessScheduleService
    {
        Task<CaseWitness> AddWitnessAsync(int userId, CreateWitnessDto dto);
        Task<WitnessCommitment> AddCommitmentAsync(int userId, CreateCommitmentDto dto);
        Task RemoveCommitmentAsync(int userId, int commitmentId);

        /// <summary>Run the conflict check for one case (after court date or availability changes).</summary>
        Task<List<WitnessConflictDto>> CheckCaseAsync(int caseId, int? actorUserId);

        Task<WitnessConflictDto> ResolveConflictAsync(int userId, ResolveConflictDto dto);
        Task<List<WitnessConflictDto>> GetActiveConflictsAsync(int? caseId = null);
        Task<List<WitnessConflictDto>> GetForCaseAsync(int caseId);
        Task<List<CaseWitness>> GetWitnessesForCaseAsync(int caseId);
    }
}
