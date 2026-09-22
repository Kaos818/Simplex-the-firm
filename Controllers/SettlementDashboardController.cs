using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.DTOs.Settlement;

namespace SimplexLawFirm.Controllers;

/// <summary>Role-separated MVC dashboard for UC-06 settlement recommendations.</summary>
[RequireSessionRole("Lawyer", "Director", "Admin")]
public sealed class SettlementDashboardController(ApplicationDbContext db, ISettlementService settlements) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var role = HttpContext.Session.GetString("UserRole");
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null) return Forbid();
        var query = db.SettlementRecommendations.Include(x => x.Case).Include(x => x.Attorney).Include(x => x.Director).AsQueryable();
        if (role == "Lawyer") query = query.Where(x => x.AttorneyId == userId.Value);
        else if (role is not ("Director" or "Admin")) return Forbid();
        ViewBag.Role = role;
        // Keep the action scoped to the signed-in attorney.  A Director can
        // request a review across active matters, while attorneys can only
        // request one for work assigned to them.
        var matters = db.Cases.Where(c => c.Status == CaseStatus.Active);
        if (role == "Lawyer") matters = matters.Where(c => c.LawyerId == userId.Value);
        ViewBag.ReviewableCases = await matters.OrderBy(c => c.CaseNumber)
            .Select(c => new { c.Id, c.CaseNumber, c.Title }).ToListAsync(ct);
        return View(await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct));
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer", "Director", "Admin")]
    public async Task<IActionResult> Evaluate(int caseId)
    {
        var role = HttpContext.Session.GetString("UserRole");
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null) return Forbid();
        var allowed = role is "Director" or "Admin"
            ? await db.Cases.AnyAsync(c => c.Id == caseId && c.Status == CaseStatus.Active)
            : await db.Cases.AnyAsync(c => c.Id == caseId && c.Status == CaseStatus.Active && c.LawyerId == userId.Value);
        if (!allowed) return Forbid();

        try
        {
            var result = await settlements.EvaluateCaseAsync(caseId, SettlementTriggerType.ManualRequest);
            TempData["Success"] = result.SettlementRecommended
                ? "Settlement analysis completed; a recommendation is ready for review."
                : "Settlement analysis completed; litigation remains the preferred outcome.";
            return RedirectToAction(nameof(Details), new { id = result.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var item = await db.SettlementRecommendations.Include(x => x.Case).Include(x => x.Attorney).Include(x => x.Director).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        var role = HttpContext.Session.GetString("UserRole"); var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || (role == "Lawyer" && item.AttorneyId != userId.Value) || role is not ("Lawyer" or "Director" or "Admin")) return Forbid();
        ViewBag.Role = role; return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> AttorneyDecision(int id, AttorneySettlementAction action, string? reasoning)
    {
        try
        {
            await settlements.ProcessAttorneyResponseAsync(HttpContext.Session.GetInt32("UserId")!.Value,
                new AttorneySettlementResponseDto { RecommendationId = id, Action = action, Response = reasoning });
            TempData["Success"] = "Settlement recommendation decision recorded.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Director", "Admin")]
    public async Task<IActionResult> DirectorDecision(int id, bool approve, string? reasoning)
    {
        try
        {
            await settlements.ProcessDirectorDecisionAsync(HttpContext.Session.GetInt32("UserId")!.Value,
                new DirectorSettlementDecisionDto { RecommendationId = id, Approve = approve, Response = reasoning ?? string.Empty });
            TempData["Success"] = "Director settlement decision recorded.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
