namespace SimplexLawFirm.DTOs.CourtFiling
{
    public class CourtFilingDetailDto
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string CaseTitle { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string SubmittedCaseNumber { get; set; } = string.Empty;
        public string SignedByAttorneyName { get; set; } = string.Empty;
        public bool SignatureDetected { get; set; }
        public string? ExpectedDocumentType { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> ValidationFailures { get; set; } = new();
        public string? ReturnReason { get; set; }
        public string? CourtReference { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public DateTime? FiledAtUtc { get; set; }
        public string? TimelineEntry { get; set; }
    }
}
