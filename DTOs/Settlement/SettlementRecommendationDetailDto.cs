namespace SimplexLawFirm.DTOs.Settlement
{
    /// <summary>
    /// Full projection of a settlement recommendation for the UI.
    /// </summary>
    public class SettlementRecommendationDetailDto
    {
        public int Id { get; set; }

        // Case info
        public int CaseId { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string CaseTitle { get; set; } = string.Empty;
        public string CaseType { get; set; } = string.Empty;

        // Trigger
        public string TriggerType { get; set; } = string.Empty;

        // Economics
        public decimal ProjectedLitigationCost { get; set; }
        public decimal LikelihoodOfSuccess { get; set; }
        public decimal MatterValue { get; set; }
        public decimal ExpectedLitigationValue { get; set; }
        public decimal AverageComparableSettlement { get; set; }
        public decimal ExpectedSettlementValue { get; set; }
        public int ComparableMatterCount { get; set; }

        // Recommendation
        public bool SettlementRecommended { get; set; }
        public string Rationale { get; set; } = string.Empty;
        public decimal? SuggestedSettlementLow { get; set; }
        public decimal? SuggestedSettlementHigh { get; set; }

        // Attorney response
        public string? AttorneyName { get; set; }
        public int? AttorneyId { get; set; }
        public string? AttorneyResponse { get; set; }
        public DateTime? AttorneyRespondedAtUtc { get; set; }

        // Director response
        public string? DirectorName { get; set; }
        public string? DirectorResponse { get; set; }
        public DateTime? DirectorDecidedAtUtc { get; set; }

        // Status
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
