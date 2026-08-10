using System.ComponentModel.DataAnnotations;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.ViewModels;

public class StartCostEstimateViewModel
{
    [Required, MaxLength(120)] public string MatterType { get; set; } = "";
}

public class CreateCostEstimateViewModel
{
    [Required, MaxLength(200)] public string ContactName { get; set; } = "";
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
    [Required, MaxLength(120)] public string MatterType { get; set; } = "";
    [Range(0, 1_000_000_000)] public decimal MatterValue { get; set; }
    public MatterUrgency Urgency { get; set; }
    public bool RequiresCourtProceedings { get; set; }
    public DocumentReadiness DocumentReadiness { get; set; }
}

public record EstimateRateSnapshot(int LawyerProfileId, string AttorneyName, decimal HourlyRate);
public record EstimateComparableSnapshot(string CaseNumber, decimal Hours, decimal BilledTotal);
public record EstimateAssumptions(decimal HistoricalAverageHours, decimal WeightedHourlyRate, decimal ValueMultiplier, decimal UrgencyMultiplier, decimal CourtMultiplier, decimal DocumentsMultiplier, decimal VatRate);
