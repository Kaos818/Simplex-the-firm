using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models;

public enum AuthorityRank { Binding, Persuasive }
public enum AuthorityTreatment { GoodLaw, Distinguished, Superseded, Overturned }
public sealed class LegalAuthority
{
    public int Id { get; set; }
    [MaxLength(300)] public string Citation { get; set; } = "";
    [MaxLength(200)] public string Subject { get; set; } = "";
    public string Summary { get; set; } = "";
    public string SearchText { get; set; } = "";
    public AuthorityRank Rank { get; set; }
    public AuthorityTreatment Treatment { get; set; }
    public bool IsInternalFallback { get; set; }
    public string? FullText { get; set; }
}
public sealed class CaseAuthorityReliance
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int LegalAuthorityId { get; set; }
    public LegalAuthority LegalAuthority { get; set; } = null!;
    public int AttorneyId { get; set; }
    public ApplicationUser Attorney { get; set; } = null!;
    public string RelevanceReason { get; set; } = "";
    public bool AdverseTreatmentConfirmed { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class ResearchQuery
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int AttorneyId { get; set; }
    public ApplicationUser Attorney { get; set; } = null!;
    public int? CaseNoteId { get; set; }
    public string Issue { get; set; } = "";
    public int ResultCount { get; set; }
    public bool LimitedToInternal { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class ResearchDisagreement
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int AttorneyId { get; set; }
    public ApplicationUser Attorney { get; set; } = null!;
    [MaxLength(300)] public string Topic { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}

public enum WhereaboutStatus { Offsite, Returned, AlertRaised, DirectorEscalated, AccountedFor, Emergency }
public sealed class AttorneyWhereabout
{
    public int Id { get; set; }
    public int AttorneyId { get; set; }
    public ApplicationUser Attorney { get; set; } = null!;
    public int? CalendarEventId { get; set; }
    public CalendarEvent? CalendarEvent { get; set; }
    public string Venue { get; set; } = "";
    public DateTime CheckedInAtUtc { get; set; }
    public DateTime ExpectedReturnAtUtc { get; set; }
    public DateTime? CheckedOutAtUtc { get; set; }
    public WhereaboutStatus Status { get; set; }
    public DateTime? AlertedAtUtc { get; set; }
    public DateTime? DirectorEscalatedAtUtc { get; set; }
    public string? ContactOutcome { get; set; }
}

public enum TrustDisbursementStatus { Draft, ManualIdentityReview, Submitted, Approved, Rejected, Paid }
public sealed class BeneficiaryTrustDisbursementRequest
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public int BeneficiaryId { get; set; }
    public Models.Beneficiaries.Beneficiary Beneficiary { get; set; } = null!;
    public int TrustAccountId { get; set; }
    public TrustAccount TrustAccount { get; set; } = null!;
    public string Purpose { get; set; } = "";
    public string Reason { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal EntitlementLimitSnapshot { get; set; }
    public decimal BalanceSnapshot { get; set; }
    public TrustDisbursementStatus Status { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public int? DecidedByUserId { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
}
