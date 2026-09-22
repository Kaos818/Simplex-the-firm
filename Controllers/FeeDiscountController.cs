using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.DTOs.FeeDiscount;
using SimplexLawFirm.Services;
using SimplexLawFirm.Data;
using System.Security.Claims;

namespace SimplexLawFirm.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeeDiscountController : ControllerBase
    {
        private readonly IFeeDiscountService _service;
        private readonly ApplicationDbContext _db;

        public FeeDiscountController(IFeeDiscountService service, ApplicationDbContext db)
        {
            _service = service;
            _db = db;
        }

        /// <summary>
        /// Client submits a fee discount request through the portal.
        /// UC-05 Steps 2-9.
        /// POST /api/FeeDiscount/submit
        /// </summary>
        [HttpPost("submit")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> SubmitDiscountRequest(
            [FromBody] SubmitFeeDiscountRequestDto dto)
        {
            try
            {
                var clientId = await GetCurrentClientIdAsync();
                var result = await _service.SubmitDiscountRequestAsync(clientId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Director decides on an escalated discount request.
        /// UC-05 Step 10.
        /// POST /api/FeeDiscount/director-decision
        /// </summary>
        [HttpPost("director-decision")]
        [Authorize(Roles = "Director")]
        public async Task<IActionResult> DirectorDecision(
            [FromBody] DirectorFeeDiscountDecisionDto dto)
        {
            try
            {
                var directorId = GetCurrentUserId();
                var result = await _service.ProcessDirectorDecisionAsync(directorId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Client responds to a Director's counter-offer.
        /// POST /api/FeeDiscount/counter-offer-response
        /// </summary>
        [HttpPost("counter-offer-response")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> RespondToCounterOffer(
            [FromBody] ClientCounterOfferResponseDto dto)
        {
            try
            {
                var clientId = GetCurrentUserId();
                var result = await _service.RespondToCounterOfferAsync(clientId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get a single discount request by ID.
        /// GET /api/FeeDiscount/{id}
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Client,Director,Accountant")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result is not null && User.IsInRole("Client") && result.ClientId != await GetCurrentClientIdAsync()) return Forbid();
            return result is null ? NotFound() : Ok(result);
        }

        /// <summary>
        /// List all discount requests for the authenticated client.
        /// GET /api/FeeDiscount/my-requests
        /// </summary>
        [HttpGet("my-requests")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetMyRequests()
        {
            var clientId = await GetCurrentClientIdAsync();
            var results = await _service.GetByClientIdAsync(clientId);
            return Ok(results);
        }

        /// <summary>
        /// List all discount requests pending Director review.
        /// GET /api/FeeDiscount/pending-review
        /// </summary>
        [HttpGet("pending-review")]
        [Authorize(Roles = "Director")]
        public async Task<IActionResult> GetPendingReview()
        {
            var results = await _service.GetPendingDirectorReviewAsync();
            return Ok(results);
        }

        // ─── Helper ───
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return int.Parse(claim.Value);
        }

        private async Task<int> GetCurrentClientIdAsync()
        {
            var userId = GetCurrentUserId();
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
                email = await _db.Users.Where(x => x.Id == userId).Select(x => x.Email).SingleOrDefaultAsync();
            var clientId = await _db.Clients.Where(x => x.Email == email).Select(x => (int?)x.Id).SingleOrDefaultAsync();
            return clientId ?? throw new UnauthorizedAccessException("The signed-in account is not linked to a client record.");
        }
    }
}
