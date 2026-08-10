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
}
public sealed record CaseReadinessReportViewModel(Case Case, IReadOnlyList<DocumentReadinessItemViewModel> Items, bool HasAuthorisedStrategy, bool CourtReady)
{
    public IReadOnlyList<DocumentReadinessItemViewModel> MissingMandatory => Items.Where(x => x.Requirement.Importance == DocumentRequirementImportance.Mandatory && !x.IsHeld && x.Waiver?.Status != DocumentWaiverStatus.Approved).ToList();
}
