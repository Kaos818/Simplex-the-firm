using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models.Beneficiaries;

public enum FacialVerificationStatus { PendingConsent, ReadyForCapture, Processing, Verified, ManualReviewRequired, FailedLiveness, FaceNotMatched, InvalidCapture, Expired, Cancelled }

public class FacialVerificationSession
{
    public Guid Id { get; set; }
    public int BeneficiaryId { get; set; }
    public Beneficiary Beneficiary { get; set; } = null!;
    public string ChallengeJson { get; set; } = "";
    public FacialVerificationStatus Status { get; set; }
    public bool ConsentGranted { get; set; }
    public DateTime? ConsentGrantedAtUtc { get; set; }
    public string ConsentNoticeVersion { get; set; } = "";
    public bool? LivenessPassed { get; set; }
    public bool? FaceMatched { get; set; }
    public decimal? SimilarityScore { get; set; }
    public decimal? ValidFrameRatio { get; set; }
    public decimal? DuplicateFrameRatio { get; set; }
    public string? ResultReasonCode { get; set; }
    public string? ResultReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
