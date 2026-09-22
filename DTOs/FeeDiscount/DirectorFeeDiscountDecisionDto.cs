using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.FeeDiscount
{
    /// <summary>
    /// Payload sent by the Director when deciding on an escalated discount request.
    /// Maps to UC-05 step 10.
    /// </summary>
    public class DirectorFeeDiscountDecisionDto
    {
        [Required]
        public int FeeDiscountRequestId { get; set; }

        /// <summary>Approve, Reject, or CounterOffer.</summary>
        [Required]
        public DirectorDecisionAction Action { get; set; }

        /// <summary>Required when Action is CounterOffer. The amount the Director proposes.</summary>
        public decimal? CounterOfferAmount { get; set; }

        /// <summary>Reason for the decision (required for Reject and CounterOffer).</summary>
        [MaxLength(2000)]
        public string? DecisionReason { get; set; }
    }

    public enum DirectorDecisionAction
    {
        Approve = 0,
        Reject = 1,
        CounterOffer = 2
    }
}
