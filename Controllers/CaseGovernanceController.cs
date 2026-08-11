using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.ViewModels;

namespace SimplexLawFirm.Controllers;

[RequireSessionRole("Lawyer", "Admin")]
public sealed class CaseGovernanceController(ApplicationDbContext db, ICaseGovernanceService governance) : Controller
{
    [RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Strategy(int caseId, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == caseId && x.LawyerId == userId, ct); if (matter is null) return Forbid();
        if (matter.Status != CaseStatus.Active)
        {
            TempData["Error"] = "Litigation strategy can only be selected for an active matter.";
            return RedirectToAction("Details", "Case", new { id = caseId });
        }
        await governance.RetestStrategyAsync(caseId, ct); ViewBag.Case = matter; ViewBag.Options = await governance.OptionsAsync(caseId, userId, ct);
        ViewBag.History = await db.LitigationStrategyDecisions.Where(x => x.CaseId == caseId).OrderByDescending(x => x.RecordedAtUtc).ToListAsync(ct);
        return View(new SelectLitigationStrategyViewModel { CaseId = caseId });
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Strategy(SelectLitigationStrategyViewModel input, CancellationToken ct)
    {
        try { var result = await governance.SelectStrategyAsync(HttpContext.Session.GetInt32("UserId")!.Value, input, ct); TempData["Success"] = result.Status == StrategyDecisionStatus.Authorised ? "Strategy authorised and recorded." : "Strategy routed for director authorisation."; return RedirectToAction(nameof(Strategy), new { caseId = input.CaseId }); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Strategy), new { caseId = input.CaseId }); }
    }

    [RequireSessionRole("Admin")]
    public async Task<IActionResult> StrategyQueue(CancellationToken ct) => View(await db.LitigationStrategyDecisions.Include(x => x.Case).Include(x => x.Attorney).Where(x => x.Status == StrategyDecisionStatus.PendingDirectorAuthorisation).OrderBy(x => x.RecordedAtUtc).ToListAsync(ct));
    [RequireSessionRole("Admin")]
    public async Task<IActionResult> StrategyReview(int id, CancellationToken ct) { var item = await db.LitigationStrategyDecisions.Include(x => x.Case).Include(x => x.Attorney).SingleOrDefaultAsync(x => x.Id == id, ct); return item is null ? NotFound() : View(item); }
    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Admin")]
    public async Task<IActionResult> DecideStrategy(int id, bool authorise, string reason, CancellationToken ct) { try { await governance.DecideStrategyAsync(HttpContext.Session.GetInt32("UserId")!.Value, id, authorise, reason, ct); TempData["Success"] = "Strategy decision recorded."; return RedirectToAction(nameof(StrategyQueue)); } catch (Exception ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(StrategyReview), new { id }); } }

    public async Task<IActionResult> Readiness(int caseId, CancellationToken ct)
    {
        try { return View(await governance.ReviewReadinessAsync(caseId, HttpContext.Session.GetInt32("UserId")!.Value, ct)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> RequestWaiver(int caseId, int requirementId, string reason, CancellationToken ct) { try { await governance.RequestWaiverAsync(HttpContext.Session.GetInt32("UserId")!.Value, caseId, requirementId, reason, ct); TempData["Success"] = "Waiver request sent to the Director."; } catch (Exception ex) { TempData["Error"] = ex.Message; } return RedirectToAction(nameof(Readiness), new { caseId }); }
    [RequireSessionRole("Admin")]
    public async Task<IActionResult> WaiverQueue(CancellationToken ct) => View(await db.CaseDocumentWaivers.Include(x => x.Case).Include(x => x.Requirement).Where(x => x.Status == DocumentWaiverStatus.PendingDirector).OrderBy(x => x.RequestedAtUtc).ToListAsync(ct));
    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Admin")]
    public async Task<IActionResult> DecideWaiver(int id, bool approve, string reason, DateTime? deadlineAtUtc, CancellationToken ct) { var waiver = await db.CaseDocumentWaivers.FindAsync([id], ct); if (waiver is null) return NotFound(); try { await governance.DecideWaiverAsync(HttpContext.Session.GetInt32("UserId")!.Value, id, approve, reason, deadlineAtUtc, ct); TempData["Success"] = deadlineAtUtc.HasValue && !approve ? "Deadline set and returned to the attorney." : "Waiver decision recorded."; } catch (Exception ex) { TempData["Error"] = ex.Message; } return RedirectToAction(nameof(Readiness), new { caseId = waiver.CaseId }); }
    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> MarkCourtReady(int caseId, CancellationToken ct) { try { await governance.MarkCourtReadyAsync(HttpContext.Session.GetInt32("UserId")!.Value, caseId, ct); TempData["Success"] = "Matter marked court-ready."; } catch (Exception ex) { TempData["Error"] = ex.Message; } return RedirectToAction(nameof(Readiness), new { caseId }); }
}
