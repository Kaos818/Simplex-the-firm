using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.Settlement
{
    /// <summary>
    /// Director's final decision on an escalated settlement recommendation (UC-06 step 9).
    /// </summary>
    public class DirectorSettlementDecisionDto
    {
        [Required]
        public int RecommendationId { get; set; }

        /// <summary>True = approve pursuing settlement; False = reject.</summary>
        [Required]
        public bool Approve { get; set; }

        /// <summary>Reason for decision.</summary>
        [Required]
        [MaxLength(4000)]
        public string Response { get; set; } = string.Empty;
    }
}
