using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;

namespace SimplexLawFirm.Controllers;

[RequireSessionUser]
public sealed class VulnerableClientController(ApplicationDbContext db, IVulnerableClientService safeguards) : Controller
{
    [RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Raise(int clientId, CancellationToken ct)
    {
        var attorneyId = HttpContext.Session.GetInt32("UserId")!.Value;
        if (!await db.Cases.AnyAsync(x => x.ClientId == clientId && x.LawyerId == attorneyId && x.Status != CaseStatus.Archived, ct))
            return Forbid();
        ViewBag.Client = await db.Clients.SingleAsync(x => x.Id == clientId, ct);
        return View(new VulnerableClientFlag { ClientId = clientId });
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Raise(int clientId, ClientSafeguard safeguard, string reason, string? languageRequired, CancellationToken ct)
    {
        try
        {
            await safeguards.RaiseAsync(clientId, HttpContext.Session.GetInt32("UserId")!.Value, safeguard, reason, languageRequired, ct);
            TempData["Success"] = "Support flag raised and sent to the Director for review. Safeguards are effective immediately.";
            return RedirectToAction("Details", "Client", new { id = clientId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException)
        { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Raise), new { clientId }); }
    }

    [RequireSessionRole("Admin")]
    public async Task<IActionResult> Queue(CancellationToken ct)
    {
        var flags = await db.VulnerableClientFlags.Include(x => x.Client).Include(x => x.RaisedByAttorney)
            .Where(x => x.Status != VulnerableFlagStatus.Removed).OrderBy(x => x.ReviewDueAtUtc).ToListAsync(ct);
        return View(flags);
    }

    [RequireSessionRole("Admin")]
    public async Task<IActionResult> Review(int id, CancellationToken ct)
    {
        var flag = await db.VulnerableClientFlags.Include(x => x.Client).Include(x => x.RaisedByAttorney)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return flag == null ? NotFound() : View(flag);
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Admin")]
    public async Task<IActionResult> Review(int id, bool confirm, string note, CancellationToken ct)
    {
        try { await safeguards.ReviewAsync(id, HttpContext.Session.GetInt32("UserId")!.Value, confirm, note, ct); TempData["Success"] = confirm ? "Safeguard confirmed." : "Safeguard removed."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Queue));
    }

    [RequireSessionRole("Admin", "Lawyer", "Paralegal", "Accountant")]
    public async Task<IActionResult> Acknowledge(int caseId, CancellationToken ct)
    {
        var staffId = HttpContext.Session.GetInt32("UserId")!.Value;
        var matter = await db.Cases.Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == caseId, ct);
        if (matter == null) return NotFound();
        ViewBag.Case = matter;
        return View(await safeguards.UnacknowledgedAsync(caseId, staffId, ct));
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Admin", "Lawyer", "Paralegal", "Accountant")]
    public async Task<IActionResult> Acknowledge(int caseId, bool understood, CancellationToken ct)
    {
        if (!understood) { TempData["Error"] = "You must acknowledge every active safeguard before opening the matter."; return RedirectToAction(nameof(Acknowledge), new { caseId }); }
        await safeguards.AcknowledgeAsync(caseId, HttpContext.Session.GetInt32("UserId")!.Value, ct);
        return RedirectToAction("Details", "Case", new { id = caseId });
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Admin", "Lawyer", "Paralegal")]
    public async Task<IActionResult> OpenSupportSession(int clientId, string supportPersonName, string purpose, CancellationToken ct)
    {
        await safeguards.OpenSupportSessionAsync(clientId, HttpContext.Session.GetInt32("UserId")!.Value, supportPersonName, purpose, ct);
        TempData["Success"] = "Supported self-service session opened for the configured period.";
        return RedirectToAction("Details", "Client", new { id = clientId });
    }

    public IActionResult SupportRequired() => View();
}
