using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.Discovery
{
    /// <summary>Director sign-off on one flagged document.</summary>
    public class PrivilegeDecisionDto
    {
        [Required]
        public int ItemId { get; set; }

        /// <summary>True = approve withholding; false = overrule and disclose.</summary>
        [Required]
        public bool Withhold { get; set; }

        [Required, MaxLength(2000)]
        public string Reason { get; set; } = string.Empty;
    }
}
