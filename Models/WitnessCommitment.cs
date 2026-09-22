using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models
{
    /// <summary>
    /// One recorded appointment, court appearance, or other commitment in a
    /// witness's schedule. Used to detect clashes with the case's court date.
    /// File: Models/WitnessCommitment.cs
    /// </summary>
    public class WitnessCommitment
    {
        public int Id { get; set; }

        public int CaseWitnessId { get; set; }
        public CaseWitness? CaseWitness { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Appointment | CourtAppearance | Personal | Other</summary>
        [Required, MaxLength(40)]
        public string Kind { get; set; } = "Other";

        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        [MaxLength(500)]
        public string? Details { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
