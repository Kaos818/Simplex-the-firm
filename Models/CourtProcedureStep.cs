using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models
{
    /// <summary>Defines the required document sequence for a case type.</summary>
    public class CourtProcedureStep
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string CaseType { get; set; } = string.Empty;

        public int SequenceNumber { get; set; }

        [Required, MaxLength(120)]
        public string DocumentType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsRequired { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }
}
