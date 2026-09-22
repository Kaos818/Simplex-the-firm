using SimplexLawFirm.DTOs.FeeDiscount;

namespace SimplexLawFirm.Services
{
    public interface IFeeDiscountService
    {
        /// <summary>Client submits a discount request (UC-05 steps 2-9).</summary>
        Task<FeeDiscountRequestDetailDto> SubmitDiscountRequestAsync(int clientId, SubmitFeeDiscountRequestDto dto);

        /// <summary>Director decides on an escalated request (UC-05 step 10).</summary>
        Task<FeeDiscountRequestDetailDto> ProcessDirectorDecisionAsync(int directorUserId, DirectorFeeDiscountDecisionDto dto);

        /// <summary>Client responds to a counter-offer.</summary>
        Task<FeeDiscountRequestDetailDto> RespondToCounterOfferAsync(int clientId, ClientCounterOfferResponseDto dto);

        /// <summary>Retrieve a single discount request with full detail.</summary>
        Task<FeeDiscountRequestDetailDto?> GetByIdAsync(int id);

        /// <summary>List all requests for a given client (portal view).</summary>
        Task<List<FeeDiscountRequestDetailDto>> GetByClientIdAsync(int clientId);

        /// <summary>List all requests pending Director review.</summary>
        Task<List<FeeDiscountRequestDetailDto>> GetPendingDirectorReviewAsync();
    }
}
