using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Storage;

namespace SimplexLawFirm.Controllers;

public sealed class DiscoveryDisclosureController(ApplicationDbContext db, IDiscoveryDisclosureService service, IDocumentStorage storage, IConfiguration configuration) : Controller
{
    [RequireSessionUser]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var role = HttpContext.Session.GetString("UserRole"); var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || role is not ("Lawyer" or "Director" or "Admin")) return Forbid();
        var query = BundleQuery(); if (role == "Lawyer") query = query.Where(x => x.AttorneyId == userId);
        ViewBag.Role = role; return View(await query.OrderByDescending(x => x.ExchangedAtUtc).ThenByDescending(x => x.Id).ToListAsync(ct));
    }

    [RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Create(int caseId, CancellationToken ct)
    {
        var attorneyId = HttpContext.Session.GetInt32("UserId")!.Value;
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == caseId && x.LawyerId == attorneyId && x.Status == CaseStatus.Active, ct);
        if (matter is null) return Forbid();
        ViewBag.Case = matter; ViewBag.Documents = await db.Documents.Where(x => x.CaseId == caseId).OrderByDescending(x => x.UploadedAt).ToListAsync(ct);
        return View();
    }

    [HttpPost, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Create(int caseId, string opposingAttorneyName, string opposingAttorneyEmail, List<int> documentIds, List<int> privilegedDocumentIds, Dictionary<int, string> privilegeReasons, CancellationToken ct)
    {
        try { var bundle = await service.CreateAsync(caseId, HttpContext.Session.GetInt32("UserId")!.Value, opposingAttorneyName, opposingAttorneyEmail, documentIds, privilegedDocumentIds.ToDictionary(x => x, x => privilegeReasons.GetValueOrDefault(x) ?? ""), ct); TempData["Success"] = bundle.Status == DiscoveryBundleStatus.AwaitingPrivilegeReview ? "Bundle created and sent to the Director for privilege review." : "Bundle created and ready to exchange."; return RedirectToAction(nameof(Details), new { id = bundle.Id }); }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Create), new { caseId }); }
    }

    [RequireSessionUser]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var bundle = await BundleQuery().SingleOrDefaultAsync(x => x.Id == id, ct); if (bundle is null) return NotFound();
        if (!CanAccess(bundle)) return Forbid(); ViewBag.Role = HttpContext.Session.GetString("UserRole"); return View(bundle);
    }

    [HttpPost, RequireSessionUser]
    public async Task<IActionResult> DecidePrivilege(int itemId, bool withhold, string reason, CancellationToken ct)
    {
        if (HttpContext.Session.GetString("UserRole") is not ("Director" or "Admin")) return Forbid();
        try { await service.DecideAsync(itemId, HttpContext.Session.GetInt32("UserId")!.Value, withhold, reason, ct); TempData["Success"] = "Privilege decision recorded."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        var bundleId = await db.DiscoveryBundleItems.Where(x => x.Id == itemId).Select(x => x.DiscoveryBundleId).SingleOrDefaultAsync(ct); return RedirectToAction(nameof(Details), new { id = bundleId });
    }

    [HttpPost, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Exchange(int id, CancellationToken ct)
    {
        var configured = configuration["Email:PublicBaseUrl"]?.TrimEnd('/'); var origin = string.IsNullOrWhiteSpace(configured) ? $"{Request.Scheme}://{Request.Host}" : configured;
        var token = await db.DiscoveryBundles.Where(x => x.Id == id).Select(x => x.ExchangeToken).SingleOrDefaultAsync(ct);
        try { await service.FinaliseAsync(id, HttpContext.Session.GetInt32("UserId")!.Value, $"{origin}/DiscoveryDisclosure/Portal?token={Uri.EscapeDataString(token ?? "")}", ct); TempData["Success"] = "The secure disclosure link was queued for the opposing attorney."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> CheckCompliance(int id, bool compliant, string notes, CancellationToken ct)
    { try { await service.SetComplianceAsync(id, HttpContext.Session.GetInt32("UserId")!.Value, compliant, notes, ct); TempData["Success"] = "Disclosure compliance review recorded."; } catch (Exception ex) { TempData["Error"] = ex.Message; } return RedirectToAction(nameof(Details), new { id }); }

    [HttpPost, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> EscalateGaps(int id, CancellationToken ct)
    { try { await service.EscalateGapsAsync(id, HttpContext.Session.GetInt32("UserId")!.Value, ct); TempData["Success"] = "A formal missing-disclosure request was queued."; } catch (Exception ex) { TempData["Error"] = ex.Message; } return RedirectToAction(nameof(Details), new { id }); }

    [AllowAnonymous, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Portal(string token, CancellationToken ct)
    { try { var bundle = await service.MarkAccessedAsync(token, ct); return View(await PortalQuery().SingleAsync(x => x.Id == bundle.Id, ct)); } catch (InvalidOperationException) { return View("Closed"); } }

    [AllowAnonymous]
    public async Task<IActionResult> Download(string token, int documentId, CancellationToken ct)
    {
        try { var bundle = await service.MarkAccessedAsync(token, ct); var item = await db.DiscoveryBundleItems.Include(x => x.Document).SingleOrDefaultAsync(x => x.DiscoveryBundleId == bundle.Id && x.DocumentId == documentId && x.PrivilegeDecision == PrivilegeDecision.OverruledDisclose, ct); if (item is null) return NotFound(); return File(await storage.OpenReadAsync(item.Document.FilePath, ct), item.Document.FileType ?? "application/octet-stream", item.Document.FileName); }
        catch (InvalidOperationException) { return NotFound(); }
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, RequestSizeLimit(150 * 1024 * 1024)]
    public async Task<IActionResult> SubmitOpposingBundle(string token, List<IFormFile> files, CancellationToken ct)
    {
        try
        {
            var bundle = await service.MarkAccessedAsync(token, ct);
            if (files.Count is < 1 or > 10 || files.Any(x => x.Length is <= 0 or > 50 * 1024 * 1024)) throw new InvalidOperationException("Upload between one and ten files, up to 50 MB each.");
            var stored = new List<(string FileName, string RelativePath, string ContentType, long SizeBytes)>();
            foreach (var file in files) { var item = await storage.StoreAsync(bundle.CaseId, file, ct); stored.Add((item.OriginalFileName, item.RelativePath, item.ContentType, item.SizeBytes)); }
            await service.RecordOpposingBundleAsync(bundle.Id, stored, ct); return View("Submitted");
        }
        catch (InvalidOperationException ex) { ViewBag.Error = ex.Message; return View("Closed"); }
    }

    [RequireSessionRole("Lawyer")]
    public async Task<IActionResult> DownloadOpposing(int id, CancellationToken ct)
    { var document = await db.OpposingDisclosureDocuments.Include(x => x.DiscoveryBundle).SingleOrDefaultAsync(x => x.Id == id, ct); if (document is null) return NotFound(); if (document.DiscoveryBundle.AttorneyId != HttpContext.Session.GetInt32("UserId")) return Forbid(); return File(await storage.OpenReadAsync(document.RelativePath, ct), document.ContentType, document.FileName); }

    private bool CanAccess(DiscoveryBundle bundle) => HttpContext.Session.GetString("UserRole") is "Director" or "Admin" || bundle.AttorneyId == HttpContext.Session.GetInt32("UserId");
    private IQueryable<DiscoveryBundle> BundleQuery() => db.DiscoveryBundles.Include(x => x.Case).Include(x => x.Attorney).Include(x => x.Items).ThenInclude(x => x.Document).Include(x => x.OpposingDocuments);
    private IQueryable<DiscoveryBundle> PortalQuery() => db.DiscoveryBundles.Include(x => x.Case).Include(x => x.Items).ThenInclude(x => x.Document).Include(x => x.OpposingDocuments);
}
