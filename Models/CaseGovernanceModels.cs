using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models;

public enum LitigationStrategyType { Settle, Negotiate, Mediate, Pursue, Trial, Withdraw }
public enum StrategyDecisionStatus { Authorised, PendingDirectorAuthorisation, Refused, RequiresReview, Superseded }
public enum DocumentRequirementImportance { Advisory, Mandatory }
public enum DocumentWaiverStatus { PendingDirector, Approved, Refused, Deferred }

public sealed class LitigationStrategyDecision
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int AttorneyId { get; set; }
    public ApplicationUser Attorney { get; set; } = null!;
    public LitigationStrategyType Strategy { get; set; }
    [MaxLength(4000)] public string Reasoning { get; set; } = "";
    [MaxLength(4000)] public string? LowProspectsJustification { get; set; }
    public decimal CostsIncurredSnapshot { get; set; }
    public decimal ProjectedCostToCompletion { get; set; }
    public decimal MatterValueSnapshot { get; set; }
    public decimal ProspectsSnapshot { get; set; }
    public decimal? ComparableSettlementLow { get; set; }
    public decimal? ComparableSettlementHigh { get; set; }
    public int ComparableMatterCount { get; set; }
    public int ExpectedDurationDays { get; set; }
    public bool CostAuthorisationRequired { get; set; }
    public bool ProspectsAuthorisationRequired { get; set; }
    public StrategyDecisionStatus Status { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewDueAtUtc { get; set; }
    public int? DirectorId { get; set; }
    public ApplicationUser? Director { get; set; }
    [MaxLength(4000)] public string? DirectorReason { get; set; }
    public DateTime? DirectorDecidedAtUtc { get; set; }
    public DateTime? SupersededAtUtc { get; set; }
}

public sealed class CaseDocumentRequirement
{
    public int Id { get; set; }
    [MaxLength(120)] public string CaseType { get; set; } = "General";
    [MaxLength(80)] public string Code { get; set; } = "";
    [MaxLength(180)] public string Name { get; set; } = "";
    [MaxLength(1000)] public string Description { get; set; } = "";
    public DocumentCategory Category { get; set; }
    public DocumentRequirementImportance Importance { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CaseDocumentWaiver
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int RequirementId { get; set; }
    public CaseDocumentRequirement Requirement { get; set; } = null!;
    public int RequestedByAttorneyId { get; set; }
    public ApplicationUser RequestedByAttorney { get; set; } = null!;
    [MaxLength(3000)] public string Reason { get; set; } = "";
    public DocumentWaiverStatus Status { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public int? DirectorId { get; set; }
    [MaxLength(3000)] public string? DirectorReason { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime? DeadlineAtUtc { get; set; }
}

public sealed class CaseReadinessReview
{
    public long Id { get; set; }
    public int CaseId { get; set; }
    public int ReviewedByUserId { get; set; }
    public int HeldCount { get; set; }
    public int MissingMandatoryCount { get; set; }
    public int MissingAdvisoryCount { get; set; }
    public bool CourtReady { get; set; }
    [MaxLength(4000)] public string SnapshotJson { get; set; } = "{}";
    public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;
}
