using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models
{
    /// <summary>
    /// UC-09: a witness linked to a case. Witnesses are not system users; their
    /// commitments are recorded as WitnessCommitment rows.
    /// File: Models/CaseWitness.cs
    /// </summary>
    public class CaseWitness
    {
        public int Id { get; set; }

        public int CaseId { get; set; }
        public Case? Case { get; set; }

        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(320)]
        public string? Email { get; set; }

        [MaxLength(40)]
        public string? Phone { get; set; }

        /// <summary>Required witnesses make a conflicting court date Critical.</summary>
        public bool IsRequired { get; set; } = true;

        public bool IsActive { get; set; } = true;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public List<WitnessCommitment> Commitments { get; set; } = new();
    }
}
