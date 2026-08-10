using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models.Beneficiaries;

public enum BeneficiaryStatus { Draft, InvitationSent, AwaitingDocuments, DocumentsRequireResubmission, AwaitingFacialVerification, UnderAutomatedReview, UnderAdminReview, Approved, Rejected, Suspended }

public class Beneficiary
{
    public int Id { get; set; }
    public int BenefactorClientId { get; set; }
    public Client BenefactorClient { get; set; } = null!;
    [MaxLength(100)] public string FirstName { get; set; } = "";
    [MaxLength(100)] public string LastName { get; set; } = "";
    [MaxLength(254)] public string Email { get; set; } = "";
    [MaxLength(30)] public string Phone { get; set; } = "";
    [MaxLength(100)] public string IdentificationNumber { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
    [MaxLength(100)] public string RelationshipToBenefactor { get; set; } = "";
    [Required, MaxLength(2000)] public string AssetAccessTerms { get; set; } = "";
    [MaxLength(1000)] public string PermittedAssetPurposes { get; set; } = "";
    [Required, MaxLength(1000)] public string EntitlementDescription { get; set; } = "";
    [Range(typeof(decimal), "0", "9999999999999999")] public decimal? EntitlementAmountLimit { get; set; }
    public DateTime? AccessEligibleFromUtc { get; set; }
    public DateTime? AccessEligibleUntilUtc { get; set; }
    public BeneficiaryStatus Status { get; set; }
    public bool PortalAccessEnabled { get; set; }
    [MaxLength(500)] public string? PortalPasswordHash { get; set; }
    public DateTime? PortalPasswordSetAtUtc { get; set; }
    [MaxLength(120)] public string? BankAccountHolder { get; set; }
    [MaxLength(100)] public string? BankName { get; set; }
    [MaxLength(40)] public string? BankAccountNumber { get; set; }
    [MaxLength(30)] public string? BankBranchCode { get; set; }
    public DateTime? BankDetailsConfirmedAtUtc { get; set; }
    [MaxLength(1000)] public string? RejectionReason { get; set; }
    [MaxLength(1000)] public string? ManualReviewReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public int? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    public ICollection<BeneficiaryRequirementAssignment> RequirementAssignments { get; set; } = [];
}
