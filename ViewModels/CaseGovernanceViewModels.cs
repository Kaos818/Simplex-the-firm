using System.ComponentModel.DataAnnotations;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.ViewModels;

public sealed class SelectLitigationStrategyViewModel
{
    public int CaseId { get; set; }
    public LitigationStrategyType Strategy { get; set; }
    [Required, StringLength(4000, MinimumLength = 20)] public string Reasoning { get; set; } = "";
    [StringLength(4000)] public string? LowProspectsJustification { get; set; }
}

public sealed record StrategyOptionViewModel(LitigationStrategyType Strategy, decimal ProjectedCost, int ExpectedDurationDays, int ComparableCount, decimal? SettlementLow, decimal? SettlementHigh);
public sealed record DocumentReadinessItemViewModel(CaseDocumentRequirement Requirement, Document? Document, ExternalEvidenceDocument? ExternalDocument, CaseDocumentWaiver? Waiver)
{
    public bool IsHeld => Document is not null || ExternalDocument is not null;
    public DateTime? SubmittedAtUtc => Document?.UploadedAt ?? ExternalDocument?.UploadedAtUtc;
    public string? FileName => Document?.FileName ?? ExternalDocument?.OriginalFileName;
}
public sealed record CaseReadinessReportViewModel(Case Case, IReadOnlyList<DocumentReadinessItemViewModel> Items, bool HasAuthorisedStrategy, bool CourtReady, long ReviewId)
{
    public IReadOnlyList<DocumentReadinessItemViewModel> MissingMandatory => Items.Where(x => x.Requirement.Importance == DocumentRequirementImportance.Mandatory && !x.IsHeld && x.Waiver?.Status != DocumentWaiverStatus.Approved).ToList();
}

/// <summary>A single requirement's state as captured in a CaseReadinessReview.SnapshotJson,
/// so a past submission report can be viewed exactly as it was decided at the time - including
/// the date each document was actually submitted - rather than reflecting the case's current
/// (possibly since-changed) document state.</summary>
public sealed record ReadinessSnapshotItem(string Code, string Name, DocumentRequirementImportance Importance, bool Held, DocumentWaiverStatus? Waiver, DateTime? SubmittedAtUtc, string? FileName);

public enum ReadinessDashboardStatus { CourtReady, Escalated, Blocked, OnTrack }

/// <summary>A row on the readiness dashboard's matter list, computed without writing an audit review.</summary>
public sealed record ReadinessDashboardRowViewModel(Case Case, DateTime? NextCourtDate, int MissingMandatoryCount, bool Escalated)
{
    public ReadinessDashboardStatus Status =>
        Case.IsCourtReady ? ReadinessDashboardStatus.CourtReady
        : Escalated ? ReadinessDashboardStatus.Escalated
        : MissingMandatoryCount > 0 ? ReadinessDashboardStatus.Blocked
        : ReadinessDashboardStatus.OnTrack;
}
