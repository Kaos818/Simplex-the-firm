using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models;

public enum KnowledgeArticleStatus { Draft, Published, Archived }
public enum PrecedentSourceType { CaseNote, CaseOutcome, KnowledgeArticle }
public enum PrecedentJobStatus { Queued, Processing, Indexed, Excluded, Failed }
public enum PrecedentFlagStatus { Pending, Retired, Amended, Retained }
public enum CoverageCommissionStatus { Commissioned, InProgress, Completed, Cancelled }

public sealed class LegalSubject
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(500)] public string Keywords { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class KnowledgeArticle
{
    public int Id { get; set; }
    [Required, MaxLength(240)] public string Title { get; set; } = "";
    [Required] public string Content { get; set; } = "";
    public KnowledgeArticleStatus Status { get; set; }
    public bool IsPrivileged { get; set; }
    public bool IsConfidential { get; set; }
    public int? SuggestedSubjectId { get; set; }
    public int AuthorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PrecedentIndexJob
{
    public int Id { get; set; }
    public PrecedentSourceType SourceType { get; set; }
    public int SourceId { get; set; }
    [Required, MaxLength(64)] public string ContentHash { get; set; } = "";
    [Required, MaxLength(240)] public string Title { get; set; } = "";
    [Required] public string SourceText { get; set; } = "";
    [MaxLength(120)] public string? MatterType { get; set; }
    public int? SuggestedSubjectId { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPrivileged { get; set; }
    public bool IsConfidential { get; set; }
    public PrecedentJobStatus Status { get; set; } = PrecedentJobStatus.Queued;
    public int AttemptCount { get; set; }
    [MaxLength(1000)] public string? LastError { get; set; }
    [MaxLength(500)] public string? ExclusionReason { get; set; }
    public DateTime QueuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class PrecedentItem
{
    public int Id { get; set; }
    public PrecedentSourceType SourceType { get; set; }
    public int SourceId { get; set; }
    [Required, MaxLength(64)] public string ContentHash { get; set; } = "";
    [Required, MaxLength(240)] public string Title { get; set; } = "";
    [Required] public string SourceText { get; set; } = "";
    public int LegalSubjectId { get; set; }
    public LegalSubject LegalSubject { get; set; } = null!;
    public bool IsCurrent { get; set; } = true;
    public DateTime SourceDateUtc { get; set; }
    public DateTime IndexedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)] public string? CuratorNote { get; set; }
    public DateTime? RetiredAtUtc { get; set; }
}

public sealed class PrecedentPassage
{
    public int Id { get; set; }
    public int PrecedentItemId { get; set; }
    public PrecedentItem PrecedentItem { get; set; } = null!;
    public int PassageNumber { get; set; }
    [Required] public string Text { get; set; } = "";
    [Required] public string EmbeddingJson { get; set; } = "";
}

public sealed class PrecedentConflictFlag
{
    public int Id { get; set; }
    public int NewPrecedentItemId { get; set; }
    public PrecedentItem NewPrecedentItem { get; set; } = null!;
    public int ExistingPrecedentItemId { get; set; }
    public PrecedentItem ExistingPrecedentItem { get; set; } = null!;
    public decimal Similarity { get; set; }
    [Required, MaxLength(600)] public string Reason { get; set; } = "";
    public PrecedentFlagStatus Status { get; set; } = PrecedentFlagStatus.Pending;
    public int? ReviewedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    [MaxLength(1000)] public string? ReviewNote { get; set; }
}

public sealed class CoverageCommission
{
    public int Id { get; set; }
    public int LegalSubjectId { get; set; }
    public LegalSubject LegalSubject { get; set; } = null!;
    public CoverageCommissionStatus Status { get; set; } = CoverageCommissionStatus.Commissioned;
    [Required, MaxLength(1000)] public string Brief { get; set; } = "";
    public int CommissionedByUserId { get; set; }
    public DateTime CommissionedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DueAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
