using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimplexLawFirm.DTOs.Settlement;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using System.Security.Claims;

namespace SimplexLawFirm.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettlementController : ControllerBase
    {
        private readonly ISettlementService _service;

        public SettlementController(ISettlementService service)
        {
            _service = service;
        }

        /// <summary>
        /// Manually trigger a settlement evaluation for a specific case.
        /// UC-06 Step 1 (manual path).
        /// POST /api/Settlement/evaluate
        /// </summary>
        [HttpPost("evaluate")]
        [Authorize(Roles = "Lawyer,Director")]
        public async Task<IActionResult> Evaluate([FromBody] TriggerSettlementEvaluationDto dto)
        {
            try
            {
                var result = await _service.EvaluateCaseAsync(
                    dto.CaseId, SettlementTriggerType.ManualRequest);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Run periodic batch evaluation across all active litigation matters.
        /// UC-06 Step 1 (scheduled). Call from a background job / Hangfire.
        /// POST /api/Settlement/run-periodic
        /// </summary>
        [HttpPost("run-periodic")]
        [Authorize(Roles = "Director")]
        public async Task<IActionResult> RunPeriodic()
        {
            var results = await _service.RunPeriodicEvaluationAsync();
            return Ok(new
            {
                evaluatedCount = results.Count,
                recommendedCount = results.Count(r => r.SettlementRecommended),
                results
            });
        }

        /// <summary>
        /// Attorney responds to a settlement recommendation.
        /// UC-06 Steps 7-8.
        /// POST /api/Settlement/attorney-response
        /// </summary>
        [HttpPost("attorney-response")]
        [Authorize(Roles = "Lawyer")]
        public async Task<IActionResult> AttorneyResponse(
            [FromBody] AttorneySettlementResponseDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _service.ProcessAttorneyResponseAsync(userId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Director decides on an escalated settlement recommendation.
        /// UC-06 Step 9.
        /// POST /api/Settlement/director-decision
        /// </summary>
        [HttpPost("director-decision")]
        [Authorize(Roles = "Director")]
        public async Task<IActionResult> DirectorDecision(
            [FromBody] DirectorSettlementDecisionDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _service.ProcessDirectorDecisionAsync(userId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get a single recommendation by ID.
        /// GET /api/Settlement/{id}
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Lawyer,Director")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result is not null && User.IsInRole("Lawyer") && result.AttorneyId != GetCurrentUserId()) return Forbid();
            return result is null ? NotFound() : Ok(result);
        }

        /// <summary>
        /// List recommendations pending the current attorney's review.
        /// GET /api/Settlement/my-pending
        /// </summary>
        [HttpGet("my-pending")]
        [Authorize(Roles = "Lawyer")]
        public async Task<IActionResult> MyPending()
        {
            var userId = GetCurrentUserId();
            return Ok(await _service.GetPendingForAttorneyAsync(userId));
        }

        /// <summary>
        /// List recommendations escalated to Director.
        /// GET /api/Settlement/pending-director
        /// </summary>
        [HttpGet("pending-director")]
        [Authorize(Roles = "Director")]
        public async Task<IActionResult> PendingDirector()
        {
            return Ok(await _service.GetPendingForDirectorAsync());
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return int.Parse(claim.Value);
        }
    }
}
