using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models
{
    /// <summary>
    /// A detected clash between a case's court date and a witness commitment.
    /// Kept as a durable record so the alert persists until resolved, and the
    /// resolution is written to the case timeline.
    /// File: Models/WitnessScheduleConflict.cs
    /// </summary>
    public class WitnessScheduleConflict
    {
        public int Id { get; set; }

        public int CaseId { get; set; }
        public Case? Case { get; set; }

        public int CaseWitnessId { get; set; }
        public CaseWitness? CaseWitness { get; set; }

        /// <summary>The court calendar event this conflict is measured against.</summary>
        public int CourtEventId { get; set; }

        /// <summary>The conflicting commitment, when the clash is with a recorded commitment.</summary>
        public int? WitnessCommitmentId { get; set; }
        public WitnessCommitment? WitnessCommitment { get; set; }

        public ConflictSeverity Severity { get; set; }
        public ConflictStatus Status { get; set; } = ConflictStatus.Active;

        public DateTime CourtDateStart { get; set; }
        public DateTime CourtDateEnd { get; set; }

        [MaxLength(500)]
        public string ConflictingEventSummary { get; set; } = string.Empty;

        public DateTime DetectedAtUtc { get; set; }

        public DateTime? ResolvedAtUtc { get; set; }

        [MaxLength(1000)]
        public string? ResolutionNotes { get; set; }

        public int? ResolvedByUserId { get; set; }
    }
}
