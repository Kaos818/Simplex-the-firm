namespace SimplexLawFirm.DTOs.FeeDiscount
{
    /// <summary>
    /// Read-only projection returned to the Director, Client portal, or audit viewers.
    /// </summary>
    public class FeeDiscountRequestDetailDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string MatterType { get; set; } = string.Empty;

        // Amounts
        public decimal OriginalAmount { get; set; }
        public decimal ProposedAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal? CounterOfferAmount { get; set; }

        // Policy snapshots
        public decimal MinimumRecoveryPercentSnapshot { get; set; }
        public decimal AutomaticApprovalPercentSnapshot { get; set; }
        public decimal EligibilityScore { get; set; }
        public string EligibilityTier { get; set; } = string.Empty;
        public decimal EligibilityLimitSnapshot { get; set; }
        public string ThresholdCheckSnapshot { get; set; } = string.Empty;
        public string? SupportingDocumentPath { get; set; }

        // Evaluation
        public string PaymentReliability { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        // Decision
        public string Status { get; set; } = string.Empty;
        public string? DecisionReason { get; set; }
        public string? DecidedByName { get; set; }
        public DateTime? DecidedAtUtc { get; set; }

        // Payment history summary (for Director review screen)
        public int InvoicesPaidOnTime { get; set; }
        public int InvoicesOverdue { get; set; }
        public decimal TotalValueBilled { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
