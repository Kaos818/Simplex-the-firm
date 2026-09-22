using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.FeeDiscount
{
    /// <summary>
    /// Payload sent by the client through the portal when requesting a fee discount.
    /// Maps to UC-05 steps 2-3.
    /// </summary>
    public class SubmitFeeDiscountRequestDto
    {
        /// <summary>The invoice the client wants a discount on.</summary>
        [Required]
        public int InvoiceId { get; set; }

        /// <summary>
        /// The amount the client proposes to pay.
        /// Must be greater than zero and less than the invoice balance.
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Proposed amount must be greater than zero.")]
        public decimal ProposedAmount { get; set; }

        [Range(0.01, 30, ErrorMessage = "Discount must be between 0.01% and 30%.")]
        public decimal? RequestedDiscountPercent { get; set; }

        /// <summary>Server-side path of a validated PDF/JPG/PNG supporting document.</summary>
        [MaxLength(500)]
        public string? SupportingDocumentPath { get; set; }

        /// <summary>Written reason / justification for the discount request.</summary>
        [Required]
        [MaxLength(2000)]
        public string Reason { get; set; } = string.Empty;
    }
}
