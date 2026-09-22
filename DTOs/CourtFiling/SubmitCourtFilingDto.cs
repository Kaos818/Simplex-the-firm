using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.CourtFiling
{
    /// <summary>Attorney payload for a court filing submission.</summary>
    public class SubmitCourtFilingDto
    {
        [Required]
        public int CaseId { get; set; }

        /// <summary>Existing Documents.Id. Upload the document through your existing document endpoint first.</summary>
        public int? DocumentId { get; set; }

        [Required, MaxLength(120)]
        public string DocumentType { get; set; } = string.Empty;

        [Required, MaxLength(80)]
        public string SubmittedCaseNumber { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string SignedByAttorneyName { get; set; } = string.Empty;

        /// <summary>Set by your document scanner or upload service after checking the file signature.</summary>
        public bool SignatureDetected { get; set; }

        [MaxLength(120)]
        public string? CourtReference { get; set; }

        [MaxLength(2000)]
        public string? FilingNotes { get; set; }
    }
}
