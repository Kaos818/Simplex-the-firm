using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.ViewModels;

namespace SimplexLawFirm.Controllers;

[RequireSessionRole("Lawyer", "Admin")]
public sealed class LegalCostRecoveryController(ApplicationDbContext db, ILegalCostRecoveryService service) : Controller
{
    [RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Create(int caseId, CancellationToken ct)
    {
        var attorneyId = HttpContext.Session.GetInt32("UserId")!.Value;
        var matter = await db.Cases.Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == caseId && x.LawyerId == attorneyId, ct);
        if (matter is null) return Forbid();
        ViewBag.Case = matter;
        ViewBag.TimeEntries = await db.TimeEntries.Where(x => x.CaseId == caseId && x.LawyerId == attorneyId)
            .Where(x => !db.LegalCostRecoveryTimeEntries.Any(link => link.TimeEntryId == x.Id)).OrderByDescending(x => x.Date).ToListAsync(ct);
        return View(new CreateLegalCostRecoveryViewModel { CaseId = caseId });
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Create(CreateLegalCostRecoveryViewModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await Create(input.CaseId, ct);
        try { var claim = await service.SubmitAsync(HttpContext.Session.GetInt32("UserId")!.Value, input, ct); TempData["Success"] = "Wasted-cost claim submitted for director review."; return RedirectToAction(nameof(Details), new { id = claim.Id }); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { ModelState.AddModelError("", ex.Message); return await Create(input.CaseId, ct); }
    }

    [RequireSessionRole("Admin")]
    public async Task<IActionResult> Queue(CancellationToken ct) => View(await db.LegalCostRecoveryClaims.Include(x => x.Case).Include(x => x.Attorney).Where(x => x.Status == LegalCostRecoveryStatus.PendingDirectorApproval).OrderBy(x => x.SubmittedAtUtc).ToListAsync(ct));

    [RequireSessionRole("Admin")]
    public async Task<IActionResult> Review(int id, CancellationToken ct)
    {
        var claim = await Query().SingleOrDefaultAsync(x => x.Id == id, ct);
        return claim is null ? NotFound() : View(claim);
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Admin")]
    public async Task<IActionResult> Decide(DecideLegalCostRecoveryViewModel input, CancellationToken ct)
    {
        try { await service.DecideAsync(HttpContext.Session.GetInt32("UserId")!.Value, input, ct); TempData["Success"] = "Director decision recorded and attorney notified."; return RedirectToAction(nameof(Details), new { id = input.ClaimId }); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Review), new { id = input.ClaimId }); }
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var claim = await Query().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (claim is null) return NotFound();
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        if (HttpContext.Session.GetString("UserRole") == "Lawyer" && claim.AttorneyId != userId) return Forbid();
        return View(claim);
    }

    private IQueryable<LegalCostRecoveryClaim> Query() => db.LegalCostRecoveryClaims.Include(x => x.Case).Include(x => x.Attorney).Include(x => x.DecidedByUser).Include(x => x.TimeEntries).ThenInclude(x => x.TimeEntry).Include(x => x.AuditEntries);
}
