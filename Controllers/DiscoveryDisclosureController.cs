using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.DTOs.Discovery;
using SimplexLawFirm.Services;
using System.Security.Claims;
using SimplexLawFirm.Infrastructure.Authorization;

namespace SimplexLawFirm.Controllers
{
    /// <summary>
    /// UC-08: Discovery and Disclosure Exchange.
    /// Attorney and Director screens live behind role auth; the opposing party's
    /// portal is anonymous but guarded by the secure, time-limited token.
    /// </summary>
    public class DiscoveryDisclosureController : Controller
    {
        private readonly IDiscoveryDisclosureService _service;
        private readonly SimplexLawFirm.Data.ApplicationDbContext _db;

        public DiscoveryDisclosureController(
            IDiscoveryDisclosureService service,
            SimplexLawFirm.Data.ApplicationDbContext db)
        {
            _service = service;
            _db = db;
        }

        // ── Attorney / Director screens ────────────────────────────────────
    [RequireSessionRole("Lawyer", "Director", "Admin")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Role = CurrentRole();
            return View(await _service.GetIndexForRoleAsync(CurrentRole(), CurrentUserId()));
        }

        [RequireSessionRole("Lawyer")]
        public async Task<IActionResult> Create(int caseId)
        {
            var matter = await _service.GetCaseForCreateAsync(caseId, CurrentUserId());
            if (matter is null) return Forbid();
            ViewBag.Case = matter;
            ViewBag.Documents = await _db.Documents
                .Where(d => d.CaseId == caseId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
            return View();
        }

        [HttpPost, RequireSessionRole("Lawyer"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateDiscoveryBundleDto dto)
        {
            try
            {
                var result = await _service.CreateBundleAsync(CurrentUserId(), dto);
                TempData["Success"] = "Disclosure bundle created.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Create), new { caseId = dto.CaseId });
            }
        }

        [RequireSessionRole("Lawyer", "Director", "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var bundle = await _service.GetDetailsAsync(id);
            if (bundle is null) return NotFound();
            if (CurrentRole() == "Lawyer" && bundle.AttorneyId != CurrentUserId()) return Forbid();
            ViewBag.Role = CurrentRole();
            ViewBag.ExchangeUrl = bundle.ExchangeToken is null ? null : $"{Request.Scheme}://{Request.Host}/DiscoveryDisclosure/Portal?token={Uri.EscapeDataString(bundle.ExchangeToken)}";
            return View(bundle);
        }

        [HttpPost, RequireSessionRole("Director", "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DecidePrivilege([FromForm] PrivilegeDecisionDto dto)
        {
            try
            {
                var result = await _service.DecidePrivilegeAsync(CurrentUserId(), dto);
                TempData["Success"] = "Privilege decision recorded.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return Redirect(Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : "/DiscoveryDisclosure");
            }
        }

        [HttpPost, RequireSessionRole("Lawyer"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Exchange(int id)
        {
            try
            {
                var result = await _service.FinaliseAndExchangeAsync(CurrentUserId(), id);
                TempData["Success"] = "Secure exchange link issued to the opposing party.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost, RequireSessionRole("Lawyer"), ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckCompliance([FromForm] ComplianceCheckDto dto)
        {
            try
            {
                var result = await _service.RecordComplianceCheckAsync(CurrentUserId(), dto);
                TempData["Success"] = "Compliance outcome recorded.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = dto.BundleId });
            }
        }

        [HttpPost, RequireSessionRole("Lawyer"), ValidateAntiForgeryToken]
        public async Task<IActionResult> EscalateGaps(int id)
        {
            try
            {
                var result = await _service.EscalateMissingDisclosureAsync(CurrentUserId(), id);
                TempData["Success"] = "Formal request for missing disclosure sent.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [RequireSessionRole("Lawyer", "Director", "Admin")]
        public async Task<IActionResult> DownloadOpposing(int id)
        {
            var (stream, fileName) = await _service.GetOpposingDocumentStreamAsync(CurrentUserId(), id, CurrentRole());
            if (stream is null) return NotFound();
            return File(stream, "application/octet-stream", fileName);
        }

        // ── External portal (no account; token is the credential) ──────────
        public async Task<IActionResult> Portal(string token)
        {
            if (!await _service.TryRecordPortalAccessAsync(token))
                return View("Closed");
            var bundle = await _service.GetPortalBundleAsync(token);
            return bundle is null ? View("Closed") : View(bundle);
        }

        public async Task<IActionResult> Download(string token, int documentId)
        {
            var (stream, fileName, contentType) = await _service.GetDisclosedDocumentStreamAsync(token, documentId);
            if (stream is null) return NotFound();
            return File(stream, contentType ?? "application/octet-stream", fileName);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(550_000_000)]
        public async Task<IActionResult> SubmitOpposingBundle(string token, List<IFormFile> files)
        {
            var payload = files.Select(f => (f.FileName, f.ContentType, f.Length, (Stream)f.OpenReadStream()));
            var (success, error) = await _service.SubmitOpposingBundleAsync(token, payload);
            if (!success)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(Portal), new { token });
            }
            return View("Submitted");
        }

        // ── Internals ──────────────────────────────────────────────────────
        private int CurrentUserId() => HttpContext.Session.GetInt32("UserId")
            ?? throw new UnauthorizedAccessException("User ID not found.");

        private string CurrentRole() => HttpContext.Session.GetString("UserRole") ?? string.Empty;
    }
}
