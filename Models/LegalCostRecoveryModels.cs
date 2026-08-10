using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models;

public enum LegalCostRecoveryGround
{
    MissedHearing,
    FrivolousOrVexatiousApplication,
    NonComplianceWithCourtOrder,
    OtherUnreasonableConduct
}

public enum LegalCostRecoveryStatus
{
    PendingDirectorApproval,
    ApprovedInFull,
    PartiallyAwarded,
    Rejected
}

public sealed class LegalCostRecoveryClaim
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int AttorneyId { get; set; }
    public ApplicationUser Attorney { get; set; } = null!;
    public LegalCostRecoveryGround Ground { get; set; }
    [MaxLength(4000)] public string Justification { get; set; } = "";
    [MaxLength(200)] public string OpposingPartyName { get; set; } = "";
    [MaxLength(320)] public string OpposingPartyEmail { get; set; } = "";
    public decimal ClaimedAmount { get; set; }
    public decimal? AwardedAmount { get; set; }
    public LegalCostRecoveryStatus Status { get; set; } = LegalCostRecoveryStatus.PendingDirectorApproval;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public int? DecidedByUserId { get; set; }
    public ApplicationUser? DecidedByUser { get; set; }
    [MaxLength(4000)] public string? DecisionNotes { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime? ServedAtUtc { get; set; }
    [MaxLength(100)] public string? ServiceDeliveryReference { get; set; }
    public ICollection<LegalCostRecoveryTimeEntry> TimeEntries { get; set; } = [];
    public ICollection<LegalCostRecoveryAuditEntry> AuditEntries { get; set; } = [];
}

public sealed class LegalCostRecoveryTimeEntry
{
    public int LegalCostRecoveryClaimId { get; set; }
    public LegalCostRecoveryClaim Claim { get; set; } = null!;
    public int TimeEntryId { get; set; }
    public TimeEntry TimeEntry { get; set; } = null!;
    public decimal AmountSnapshot { get; set; }
}

public sealed class LegalCostRecoveryAuditEntry
{
    public long Id { get; set; }
    public int LegalCostRecoveryClaimId { get; set; }
    public LegalCostRecoveryClaim Claim { get; set; } = null!;
    public int ActorUserId { get; set; }
    [MaxLength(100)] public string Action { get; set; } = "";
    [MaxLength(2000)] public string Details { get; set; } = "";
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
