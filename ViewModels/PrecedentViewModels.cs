using SimplexLawFirm.Models;

namespace SimplexLawFirm.ViewModels;

public sealed record SubjectCoverageRow(LegalSubject Subject, int CurrentItems, int TotalItems, CoverageCommission? OpenCommission);
public sealed class PrecedentDashboardViewModel
{
    public IReadOnlyList<SubjectCoverageRow> Coverage { get; init; } = [];
    public IReadOnlyList<PrecedentConflictFlag> Flags { get; init; } = [];
    public IReadOnlyList<PrecedentConflictFlag> ReviewedFlags { get; init; } = [];
    public IReadOnlyList<PrecedentIndexJob> Backlog { get; init; } = [];
    public IReadOnlyList<PrecedentItem> RecentItems { get; init; } = [];
    public int ExcludedCount { get; init; }
    public int IndexedCount { get; init; }
}
