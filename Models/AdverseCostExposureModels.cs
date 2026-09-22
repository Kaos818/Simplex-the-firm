using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models;

public enum AdverseCostExposureStatus { Open, Negotiation, Taxation, DirectorReviewed, Resolved }

public sealed class AdverseCostExposure
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int CourtOrderDocumentId { get; set; }
    public Document CourtOrderDocument { get; set; } = null!;
    public int LoggedByAttorneyId { get; set; }
    public ApplicationUser LoggedByAttorney { get; set; } = null!;
    [MaxLength(200)] public string IssuingCourt { get; set; } = "";
    public DateTime RulingDateUtc { get; set; }
    public decimal CostOrderAmount { get; set; }
    public decimal ExistingCaseCostsSnapshot { get; set; }
    public decimal TotalExposure { get; set; }
    public decimal AvailableTrustRetainerSnapshot { get; set; }
    public decimal Shortfall { get; set; }
    public decimal? RevisedAmount { get; set; }
    public decimal? FinalSettledAmount { get; set; }
    public AdverseCostExposureStatus Status { get; set; } = AdverseCostExposureStatus.Open;
    [MaxLength(2000)] public string? ProgressNote { get; set; }
    public int? AccountantReviewedByUserId { get; set; }
    public ApplicationUser? AccountantReviewedByUser { get; set; }
    public DateTime? AccountantReviewedAtUtc { get; set; }
    public int? DirectorReviewedByUserId { get; set; }
    public ApplicationUser? DirectorReviewedByUser { get; set; }
    [MaxLength(2000)] public string? DirectorPosition { get; set; }
    public DateTime? DirectorReviewedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
}
