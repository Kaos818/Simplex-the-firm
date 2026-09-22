using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.Discovery
{
    /// <summary>Attorney outcome of comparing received disclosure against legal requirements.</summary>
    public class ComplianceCheckDto
    {
        [Required]
        public int BundleId { get; set; }

        /// <summary>True = Compliant; false = Gaps Identified.</summary>
        [Required]
        public bool Compliant { get; set; }

        /// <summary>Documents believed missing, or confirmation notes.</summary>
        [Required, MaxLength(4000)]
        public string Notes { get; set; } = string.Empty;
    }
}
