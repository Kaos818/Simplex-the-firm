using Microsoft.AspNetCore.Mvc;
using SimplexLawFirm.DTOs.Witnesses;
using SimplexLawFirm.Services;
using SimplexLawFirm.Data;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Infrastructure.Authorization;

namespace SimplexLawFirm.Controllers
{
    /// <summary>
    /// UC-09: Detect Witness Schedule Conflicts. Attorneys manage witnesses and
    /// their commitments; the system raises and clears conflict alerts.
    /// </summary>
    [RequireSessionRole("Lawyer", "Director", "Admin")]
    public class WitnessScheduleController : Controller
    {
        private readonly IWitnessScheduleService _service;
        private readonly ApplicationDbContext _db;

        public WitnessScheduleController(IWitnessScheduleService service, ApplicationDbContext db) { _service = service; _db = db; }

        // Attorney: all active conflicts across their matters.
        public async Task<IActionResult> Index()
        {
            var conflicts = await _service.GetActiveConflictsAsync();
            if (CurrentRole() == "Lawyer")
            {
                var caseIds = await _db.Cases.Where(x => x.LawyerId == CurrentUserId()).Select(x => x.Id).ToListAsync();
                conflicts = conflicts.Where(x => caseIds.Contains(x.CaseId)).ToList();
            }
            return View(conflicts);
        }

        // Per-case witness register with commitments and conflicts.
        public async Task<IActionResult> Case(int caseId)
        {
            if (CurrentRole() == "Lawyer" && !await _db.Cases.AnyAsync(x => x.Id == caseId && x.LawyerId == CurrentUserId())) return Forbid();
            ViewBag.CaseId = caseId;
            ViewBag.Conflicts = await _service.GetForCaseAsync(caseId);
            return View(await _service.GetWitnessesForCaseAsync(caseId));
        }

        [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
        public async Task<IActionResult> AddWitness(CreateWitnessDto dto)
        {
            try { await _service.AddWitnessAsync(CurrentUserId(), dto); TempData["Success"] = "Witness added."; }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Case), new { caseId = dto.CaseId });
        }

        [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
        public async Task<IActionResult> AddCommitment(CreateCommitmentDto dto)
        {
            try
            {
                await _service.AddCommitmentAsync(CurrentUserId(), dto);
                TempData["Success"] = "Commitment recorded; schedules rechecked.";
            }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return Redirect(Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : "/WitnessSchedule");
        }

        [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
        public async Task<IActionResult> RemoveCommitment(int commitmentId)
        {
            try { await _service.RemoveCommitmentAsync(CurrentUserId(), commitmentId); TempData["Success"] = "Commitment removed; schedules rechecked."; }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return Redirect(Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : "/WitnessSchedule");
        }

        /// <summary>Manual recheck (also called automatically after any change).</summary>
        [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
        public async Task<IActionResult> Recheck(int caseId)
        {
            try { await _service.CheckCaseAsync(caseId, CurrentUserId()); TempData["Success"] = "Schedules rechecked."; }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Case), new { caseId });
        }

        [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
        public async Task<IActionResult> Resolve(ResolveConflictDto dto)
        {
            try
            {
                var result = await _service.ResolveConflictAsync(CurrentUserId(), dto);
                TempData["Success"] = "Conflict resolved and recorded in the case timeline.";
                return RedirectToAction(nameof(Case), new { caseId = result.CaseId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return Redirect(Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : "/WitnessSchedule");
            }
        }

        private int CurrentUserId() => HttpContext.Session.GetInt32("UserId")
            ?? throw new UnauthorizedAccessException("User ID not found.");
        private string CurrentRole() => HttpContext.Session.GetString("UserRole") ?? string.Empty;
    }
}
