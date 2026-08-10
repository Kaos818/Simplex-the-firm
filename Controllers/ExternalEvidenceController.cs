using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Security;
using SimplexLawFirm.Services.Storage;

namespace SimplexLawFirm.Controllers;

public sealed class ExternalEvidenceController(ApplicationDbContext db, IEmailService email, ExternalEvidenceStorage storage, IConfiguration config) : Controller
{
    private const string RequestKey = "ExternalEvidenceRequestId";
    private const string SessionExpiryKey = "ExternalEvidenceSessionExpiry";

    [RequireSessionRole("Lawyer", "Admin")]
    public async Task<IActionResult> RequestEvidence(int caseId, CancellationToken ct)
    {
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == caseId, ct);
        return matter is null || !MayManage(matter) ? Forbid() : View(matter);
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer", "Admin")]
    public async Task<IActionResult> RequestEvidence(int caseId, string recipientName, string recipientEmail, CancellationToken ct)
    {
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == caseId, ct);
        if (matter is null || !MayManage(matter)) return Forbid();
        if (string.IsNullOrWhiteSpace(recipientName) || string.IsNullOrWhiteSpace(recipientEmail)) return BadRequest();
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var open = await db.ExternalEvidenceRequests.Where(x => x.CaseId == caseId && x.RevokedAtUtc == null && x.ClosedAtUtc == null).ToListAsync(ct);
        open.ForEach(x => x.RevokedAtUtc = DateTime.UtcNow);
        var token = SecureToken.Create();
        var request = new ExternalEvidenceRequest { CaseId = caseId, RecipientName = recipientName.Trim(), RecipientEmail = recipientEmail.Trim(), TokenHash = token.Hash, RequestedByUserId = userId, ExpiresAtUtc = DateTime.UtcNow.AddDays(7) };
        db.Add(request);
        await db.SaveChangesAsync(ct);
        var configuredBase = config["Email:PublicBaseUrl"]?.TrimEnd('/');
        var baseUrl = string.IsNullOrWhiteSpace(configuredBase) ? $"{Request.Scheme}://{Request.Host}" : configuredBase;
        var url = $"{baseUrl}/ExternalEvidence/Open?token={Uri.EscapeDataString(token.Raw)}";
        var name = System.Net.WebUtility.HtmlEncode(request.RecipientName);
        var encodedUrl = System.Net.WebUtility.HtmlEncode(url);
        var html = $"<div style='font-family:Arial;background:#eef5f4;padding:32px'><div style='max-width:620px;margin:auto;background:#fff;padding:34px;border-radius:18px;border-top:6px solid #0f766e'><h1 style='color:#0b2b37'>Secure documentation request</h1><p>Hello {name},</p><p>The presiding legal team requests documents relevant to matter <strong>{System.Net.WebUtility.HtmlEncode(matter.CaseNumber)}</strong>. State clearly what each document supports.</p><p><a href='{encodedUrl}' style='display:inline-block;padding:14px 22px;background:#0f766e;color:#fff;text-decoration:none;border-radius:9px;font-weight:bold'>Open one-time portal</a></p><p style='color:#52666d'>The link expires in seven days and can be opened once. Submission permanently closes the portal.</p></div></div>";
        await email.QueueAsync(request.RecipientEmail, $"Secure document request - {matter.CaseNumber}", html, $"Secure document request for {matter.CaseNumber}: {url}", $"external-evidence:{request.Id}", ct);
        db.AuditEntries.Add(new() { ActorUserId = userId, EntityType = "ExternalEvidenceRequest", EntityId = request.Id.ToString(), Action = "External evidence request sent" });
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "The one-time documentation link was queued for delivery.";
        return RedirectToAction(nameof(Received), new { caseId });
    }

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Open(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return View("Closed");
        var item = await db.ExternalEvidenceRequests.Include(x => x.Case).SingleOrDefaultAsync(x => x.TokenHash == SecureToken.Hash(token), ct);
        if (item is null || item.ExpiresAtUtc <= DateTime.UtcNow || item.RevokedAtUtc != null || item.AccessedAtUtc != null || item.ClosedAtUtc != null) return View("Closed");
        item.AccessedAtUtc = DateTime.UtcNow;
        HttpContext.Session.SetString(RequestKey, item.Id.ToString());
        HttpContext.Session.SetString(SessionExpiryKey, DateTime.UtcNow.AddMinutes(20).ToString("O"));
        await db.SaveChangesAsync(ct);
        ViewBag.Requirements = await db.CaseDocumentRequirements.Where(x => x.IsActive && (x.CaseType == item.Case.CaseType || x.CaseType == "General")).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(55_000_000)]
    public async Task<IActionResult> Upload(List<IFormFile> files, List<string> purposes, List<string> requirementCodes, CancellationToken ct)
    {
        if (!TryPortalRequestId(out var id) || files.Count is 0 or > 5 || files.Count != purposes.Count || files.Count != requirementCodes.Count) return View("Closed");
        var item = await db.ExternalEvidenceRequests.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null || item.ClosedAtUtc != null || item.RevokedAtUtc != null || item.ExpiresAtUtc <= DateTime.UtcNow) return View("Closed");
        try
        {
            for (var index = 0; index < files.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(purposes[index])) return BadRequest("State the purpose for every document.");
                var saved = await storage.StoreAsync(item.Id, files[index], ct);
                db.Add(new ExternalEvidenceDocument { RequestId = item.Id, OriginalFileName = saved.Name, Purpose = purposes[index].Trim(), RequirementCode = string.IsNullOrWhiteSpace(requirementCodes[index]) ? null : requirementCodes[index], RelativePath = saved.Path, ContentType = saved.Type, SizeBytes = saved.Size, Sha256Hash = saved.Hash });
            }
        }
        catch (InvalidDataException ex) { return BadRequest(ex.Message); }
        item.ClosedAtUtc = DateTime.UtcNow;
        db.AuditEntries.Add(new() { EntityType = "ExternalEvidenceRequest", EntityId = item.Id.ToString(), Action = $"External evidence submitted ({files.Count} document(s))" });
        await db.SaveChangesAsync(ct);
        ClosePortalSession();
        return View("Submitted");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Abandon(CancellationToken ct)
    {
        if (TryPortalRequestId(out var id))
        {
            var item = await db.ExternalEvidenceRequests.SingleOrDefaultAsync(x => x.Id == id && x.ClosedAtUtc == null, ct);
            if (item is not null) { item.ClosedAtUtc = DateTime.UtcNow; db.AuditEntries.Add(new() { EntityType = "ExternalEvidenceRequest", EntityId = id.ToString(), Action = "External portal abandoned and permanently closed" }); await db.SaveChangesAsync(ct); }
        }
        ClosePortalSession(); return NoContent();
    }

    [RequireSessionRole("Lawyer", "Admin")]
    public async Task<IActionResult> Received(int caseId, CancellationToken ct)
    {
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == caseId, ct);
        if (matter is null || !MayManage(matter)) return Forbid();
        ViewBag.Case = matter;
        return View(await db.ExternalEvidenceRequests.Include(x => x.Documents).Where(x => x.CaseId == caseId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct));
    }

    [RequireSessionRole("Lawyer", "Admin")]
    public async Task<IActionResult> Download(long id, CancellationToken ct)
    {
        var document = await db.ExternalEvidenceDocuments.Include(x => x.Request).ThenInclude(x => x.Case).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (document is null) return NotFound();
        if (!MayManage(document.Request.Case)) return Forbid();
        return File(await storage.OpenReadAsync(document.RelativePath, ct), document.ContentType, document.OriginalFileName);
    }

    private bool MayManage(Case matter) => HttpContext.Session.GetString("UserRole") == "Admin" || matter.LawyerId == HttpContext.Session.GetInt32("UserId");
    private bool TryPortalRequestId(out long id)
    {
        id = 0;
        var validExpiry = DateTime.TryParse(HttpContext.Session.GetString(SessionExpiryKey), out var expiry) && expiry > DateTime.UtcNow;
        return validExpiry && long.TryParse(HttpContext.Session.GetString(RequestKey), out id);
    }
    private void ClosePortalSession() { HttpContext.Session.Remove(RequestKey); HttpContext.Session.Remove(SessionExpiryKey); }
}
