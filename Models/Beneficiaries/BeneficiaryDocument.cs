namespace SimplexLawFirm.Models.Beneficiaries;

public enum DocumentPreScreenStatus { Pending, Processing, Passed, ManualReviewRequired, ResubmissionRequired, FailedTechnicalProcessing }

public class BeneficiaryDocument
{
    public long Id { get; set; }
    public int BeneficiaryId { get; set; }
    public Beneficiary Beneficiary { get; set; } = null!;
    public int RequirementId { get; set; }
    public BeneficiaryDocumentRequirement Requirement { get; set; } = null!;
    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public string RelativeStoragePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Sha256Hash { get; set; } = "";
    public DocumentPreScreenStatus PreScreenStatus { get; set; }
    public decimal? QualityScore { get; set; }
    public decimal? OcrConfidence { get; set; }
    public bool? CertificationWordingDetected { get; set; }
    public bool? CertificationStampDetected { get; set; }
    public bool? SignatureDetected { get; set; }
    public DateTime? DetectedCertificationDate { get; set; }
    public DateTime? DetectedExpiryDate { get; set; }
    public string? ExtractedDocumentType { get; set; }
    public string? ReasonCode { get; set; }
    public string? UserFacingReason { get; set; }
    public string? TechnicalResultJson { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AnalysedAtUtc { get; set; }
}
