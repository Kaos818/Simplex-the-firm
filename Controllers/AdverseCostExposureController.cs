using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;

namespace SimplexLawFirm.Controllers;

[RequireSessionUser]
public sealed class AdverseCostExposureController(ApplicationDbContext db, IAdverseCostExposureService service) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var role = HttpContext.Session.GetString("UserRole"); var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || role is not ("Lawyer" or "Accountant" or "Director" or "Admin")) return Forbid();
        var query = Query(); if (role == "Lawyer") query = query.Where(x => x.LoggedByAttorneyId == userId);
        ViewBag.Role = role; return View(await query.OrderBy(x => x.Status == AdverseCostExposureStatus.Resolved).ThenByDescending(x => x.TotalExposure).ToListAsync(ct));
    }

    [RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Create(int caseId, CancellationToken ct)
    {
        var lawyerId = HttpContext.Session.GetInt32("UserId")!.Value;
        var matter = await db.Cases.Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == caseId && x.LawyerId == lawyerId && x.Status == CaseStatus.Active, ct); if (matter is null) return Forbid();
        ViewBag.Case = matter; ViewBag.CourtOrders = await db.Documents.Where(x => x.CaseId == caseId && x.Category == DocumentCategory.CourtOrders).OrderByDescending(x => x.UploadedAt).ToListAsync(ct); return View(new AdverseCostExposure { CaseId = caseId, RulingDateUtc = DateTime.UtcNow.Date });
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Create(AdverseCostExposure input, CancellationToken ct)
    {
        try { var item = await service.LogAsync(input, HttpContext.Session.GetInt32("UserId")!.Value, ct); TempData["Success"] = "Adverse cost order logged; the exposure and retainer shortfall were calculated."; return RedirectToAction(nameof(Details), new { id = item.Id }); }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Create), new { caseId = input.CaseId }); }
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var item = await Query().SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound();
        var role = HttpContext.Session.GetString("UserRole"); var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || (role == "Lawyer" && item.LoggedByAttorneyId != userId) || role is not ("Lawyer" or "Accountant" or "Director" or "Admin")) return Forbid();
        ViewBag.Role = role; return View(item);
    }

    [HttpPost, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Progress(int id, AdverseCostExposureStatus status, decimal? revisedAmount, string note, CancellationToken ct)
    { try { await service.UpdateProgressAsync(id, HttpContext.Session.GetInt32("UserId")!.Value, status, revisedAmount, note, ct); TempData["Success"] = "Negotiation/taxation progress recorded."; } catch (Exception ex) { TempData["Error"] = ex.Message; } return RedirectToAction(nameof(Details), new { id }); }

    [HttpPost, RequireSessionRole("Accountant")]
    public async Task<IActionResult> AccountantReview(int id, string note, CancellationToken ct)
    { try { await service.ReviewByAccountantAsync(id, HttpContext.Session.GetInt32("UserId")!.Value, note, ct); TempData["Success"] = "Exposure and retainer shortfall review recorded."; } catch (Exception ex) { TempData["Error"] = ex.Message; } return RedirectToAction(nameof(Details), new { id }); }

    [HttpPost]
    public async Task<IActionResult> DirectorReview(int id, string position, CancellationToken ct)
    { if (HttpContext.Session.GetString("UserRole") is not ("Director" or "Admin")) return Forbid(); try { await service.ConfirmDirectorPositionAsync(id, HttpContext.Session.GetInt32("UserId")!.Value, position, ct); TempData["Success"] = "The Director's position was recorded."; } catch (Exception ex) { TempData["Error"] = ex.Message; } return RedirectToAction(nameof(Details), new { id }); }

    [HttpPost, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Resolve(int id, decimal settledAmount, string note, CancellationToken ct)
    { try { await service.ResolveAsync(id, HttpContext.Session.GetInt32("UserId")!.Value, settledAmount, note, ct); TempData["Success"] = "Exposure closed with the final settled amount."; } catch (Exception ex) { TempData["Error"] = ex.Message; } return RedirectToAction(nameof(Details), new { id }); }

    private IQueryable<AdverseCostExposure> Query() => db.AdverseCostExposures.Include(x => x.Case).Include(x => x.CourtOrderDocument).Include(x => x.LoggedByAttorney).Include(x => x.AccountantReviewedByUser).Include(x => x.DirectorReviewedByUser);
}
