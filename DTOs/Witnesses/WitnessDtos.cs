using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.Witnesses
{
    public class CreateWitnessDto
    {
        [Required] public int CaseId { get; set; }
        [Required, MaxLength(200)] public string FullName { get; set; } = string.Empty;
        [EmailAddress, MaxLength(320)] public string? Email { get; set; }
        [MaxLength(40)] public string? Phone { get; set; }
        public bool IsRequired { get; set; } = true;
        [MaxLength(1000)] public string? Notes { get; set; }
    }

    public class CreateCommitmentDto
    {
        [Required] public int CaseWitnessId { get; set; }
        [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
        [Required, MaxLength(40)] public string Kind { get; set; } = "Other";
        [Required] public DateTime StartDateTime { get; set; }
        [Required] public DateTime EndDateTime { get; set; }
        [MaxLength(500)] public string? Details { get; set; }
    }

    public class ResolveConflictDto
    {
        [Required] public int ConflictId { get; set; }
        [Required, MaxLength(1000)] public string ResolutionNotes { get; set; } = string.Empty;
    }

    public class WitnessConflictDto
    {
        public int ConflictId { get; set; }
        public int CaseId { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string WitnessName { get; set; } = string.Empty;
        public bool WitnessIsRequired { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CourtDateStart { get; set; }
        public string ConflictingEventSummary { get; set; } = string.Empty;
        public DateTime DetectedAtUtc { get; set; }
        public string? ResolutionNotes { get; set; }
    }
}
