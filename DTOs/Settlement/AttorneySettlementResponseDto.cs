using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.Settlement
{
    /// <summary>
    /// Attorney's response to a settlement recommendation (UC-06 step 8).
    /// </summary>
    public class AttorneySettlementResponseDto
    {
        [Required]
        public int RecommendationId { get; set; }

        /// <summary>Accept, Reject, or Escalate.</summary>
        [Required]
        public AttorneySettlementAction Action { get; set; }

        /// <summary>Required for Reject and Escalate; optional for Accept.</summary>
        [MaxLength(4000)]
        public string? Response { get; set; }
    }

    public enum AttorneySettlementAction
    {
        Accept = 0,
        Reject = 1,
        Escalate = 2
    }
}
