using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplexLawFirm.Models
{
    /// <summary>
    /// Court filing queue record for UC-07. This stores the validation evidence,
    /// return reasons, filing timestamp, and case timeline information.
    /// File: Models/CourtFilingSubmission.cs
    /// </summary>
    public class CourtFilingSubmission
    {
        public int Id { get; set; }

        public int CaseId { get; set; }
        public Case? Case { get; set; }

        public int SubmittedByAttorneyId { get; set; }
        public ApplicationUser? SubmittedByAttorney { get; set; }

        public int? DocumentId { get; set; }
        public Document? Document { get; set; }

        [Required, MaxLength(120)]
        public string DocumentType { get; set; } = string.Empty;

        [Required, MaxLength(80)]
        public string SubmittedCaseNumber { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string SignedByAttorneyName { get; set; } = string.Empty;

        /// <summary>Captured from the uploaded document's signature/metadata check.</summary>
        public bool SignatureDetected { get; set; }

        /// <summary>Attorney of record at submission time.</summary>
        public int? AttorneyOfRecordId { get; set; }
        public ApplicationUser? AttorneyOfRecord { get; set; }

        /// <summary>Expected next document type from the procedural sequence.</summary>
        [MaxLength(120)]
        public string? ExpectedDocumentType { get; set; }

        public CourtFilingStatus Status { get; set; }

        /// <summary>JSON array of all failed validation checks.</summary>
        public string ValidationFailuresJson { get; set; } = "[]";

        [MaxLength(4000)]
        public string? ReturnReason { get; set; }

        public DateTime SubmittedAtUtc { get; set; }
        public DateTime? ValidatedAtUtc { get; set; }
        public DateTime? FiledAtUtc { get; set; }
        public DateTime? ReturnedAtUtc { get; set; }

        public int? ValidatedByUserId { get; set; }
        public ApplicationUser? ValidatedByUser { get; set; }

        [MaxLength(120)]
        public string? CourtReference { get; set; }

        [MaxLength(2000)]
        public string? FilingNotes { get; set; }

        /// <summary>Human-readable case timeline entry generated when filing succeeds.</summary>
        [MaxLength(1000)]
        public string? TimelineEntry { get; set; }
    }
}
