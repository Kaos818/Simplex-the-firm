using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.FeeDiscount
{
    /// <summary>
    /// Payload sent by the client when responding to a Director's counter-offer.
    /// </summary>
    public class ClientCounterOfferResponseDto
    {
        [Required]
        public int FeeDiscountRequestId { get; set; }

        /// <summary>True = accept the counter-offer, False = reject it.</summary>
        [Required]
        public bool Accept { get; set; }
    }
}
