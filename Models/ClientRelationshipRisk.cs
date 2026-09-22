using System.ComponentModel.DataAnnotations;
namespace SimplexLawFirm.Models;
public sealed class ClientRelationshipRisk
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public decimal FinancialScore { get; set; }
    public decimal LegalScore { get; set; }
    public decimal ReputationScore { get; set; }
    public decimal PredictedRelationshipScore { get; set; }
    public decimal OverallScore { get; set; }
    [MaxLength(2000)] public string Explanation { get; set; } = "";
    public bool HighRiskAlert { get; set; }
    [MaxLength(100)] public string? DirectorAction { get; set; }
    [MaxLength(2000)] public string? DirectorNotes { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}
