using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.Settlement
{
    /// <summary>
    /// Manually trigger a settlement evaluation for a case (UC-06 step 1 manual path).
    /// </summary>
    public class TriggerSettlementEvaluationDto
    {
        [Required]
        public int CaseId { get; set; }
    }
}
