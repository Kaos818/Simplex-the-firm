using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models;

/// <summary>Firm-wide, configurable weighting for the legal-matter priority calculation.</summary>
public sealed class CasePriorityWeightConfiguration
{
    public int Id { get; set; }
    [Range(0, 100)] public decimal RiskWeight { get; set; } = 30m;
    [Range(0, 100)] public decimal UrgencyWeight { get; set; } = 25m;
    [Range(0, 100)] public decimal ComplexityWeight { get; set; } = 15m;
    [Range(0, 100)] public decimal DocumentReadinessWeight { get; set; } = 15m;
    [Range(0, 100)] public decimal FinancialExposureWeight { get; set; } = 15m;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? UpdatedByUserId { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
}

public enum CasePriorityBand { Low, Medium, High, Critical }

public sealed record CasePriorityFactor(string Name, decimal Score, decimal Weight, string Explanation)
{
    public decimal Contribution => Math.Round(Score * Weight / 100m, 1);
}

public sealed record CasePriorityResult(
    int CaseId,
    string CaseNumber,
    string CaseTitle,
    string ClientName,
    CasePriorityBand Band,
    decimal Score,
    IReadOnlyList<CasePriorityFactor> Factors);
