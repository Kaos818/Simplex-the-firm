using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimplexLawFirm.DTOs.CourtFiling;
using SimplexLawFirm.Services;
using System.Security.Claims;

namespace SimplexLawFirm.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Lawyer,Director")]
    public class CourtFilingQueueController : ControllerBase
    {
        private readonly ICourtFilingQueueService _service;

        public CourtFilingQueueController(ICourtFilingQueueService service) => _service = service;

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] SubmitCourtFilingDto dto)
        {
            try { return Ok(await _service.SubmitAsync(CurrentUserId(), dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateCourtFilingDto dto)
        {
            try { return Ok(await _service.ValidateAsync(CurrentUserId(), dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("return")]
        public async Task<IActionResult> Return([FromBody] ReturnCourtFilingDto dto)
        {
            try { return Ok(await _service.ReturnToAttorneyAsync(CurrentUserId(), dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("{id:int}/file")]
        public async Task<IActionResult> MarkFiled(int id, [FromQuery] string? courtReference)
        {
            try { return Ok(await _service.MarkFiledAsync(CurrentUserId(), id, courtReference)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpGet("queue")]
        public async Task<IActionResult> Queue([FromQuery] int? caseId = null) => Ok(await _service.GetQueueAsync(caseId));

        [HttpGet("case/{caseId:int}")]
        public async Task<IActionResult> ForCase(int caseId) => Ok(await _service.GetForCaseAsync(caseId));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }

        private int CurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            return claim is null ? throw new UnauthorizedAccessException("User ID not found in token.") : int.Parse(claim.Value);
        }
    }
}
