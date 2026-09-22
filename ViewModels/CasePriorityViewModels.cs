using SimplexLawFirm.Models;

namespace SimplexLawFirm.ViewModels;

public sealed class CasePriorityHeatmapViewModel
{
    public IReadOnlyList<CasePriorityResult> Matters { get; init; } = [];
    public CasePriorityWeightConfiguration Configuration { get; init; } = new();
    public bool CanConfigure { get; init; }
}
