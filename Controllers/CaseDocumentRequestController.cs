using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.CurrentUser;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services.Storage;

namespace SimplexLawFirm.Controllers;

[RequireSessionUser, AutoValidateAntiforgeryToken]
public class CaseDocumentRequestController(ApplicationDbContext db, ICurrentClientService currentClient, INotificationService notifications, IDocumentStorage storage) : Controller
{
    [HttpGet, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> SelectCase(CancellationToken ct)
    {
        var lawyerId = HttpContext.Session.GetInt32("UserId")!.Value;
        return View(await db.Cases.Include(x => x.Client)
            .Where(x => x.LawyerId == lawyerId && x.Status == CaseStatus.Active)
            .OrderBy(x => x.CaseNumber).ToListAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var role = HttpContext.Session.GetString("UserRole");
        var userId = HttpContext.Session.GetInt32("UserId");
        var query = Query();
        if (role == "Lawyer" && userId.HasValue)
            query = query.Where(x => x.RequestedByAttorneyId == userId.Value || x.Case.LawyerId == userId.Value);
        else if (role == "Client")
        {
            var client = await currentClient.GetAsync(ct);
            if (client is null) return Forbid();
            query = query.Where(x => x.ClientId == client.Id);
        }
        else if (role is not ("Admin" or "Director")) return Forbid();

        return View(await query.OrderBy(x => x.Status == CaseDocumentRequestStatus.Accepted)
            .ThenBy(x => x.DueAtUtc).ThenByDescending(x => x.CreatedAtUtc).ToListAsync(ct));
    }

    [RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Create(int caseId, CancellationToken ct)
    {
        var lawyerId = HttpContext.Session.GetInt32("UserId")!.Value;
        var matter = await db.Cases.Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == caseId && x.LawyerId == lawyerId && x.Status == CaseStatus.Active, ct);
        if (matter is null) return NotFound();
        ViewBag.Case = matter;
        return View(new CaseDocumentRequest { CaseId = caseId, ClientId = matter.ClientId, DueAtUtc = DateTime.UtcNow.AddDays(7) });
    }

    [HttpPost, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Create(CaseDocumentRequest input, CancellationToken ct)
    {
        var lawyerId = HttpContext.Session.GetInt32("UserId")!.Value;
        var matter = await db.Cases.Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == input.CaseId && x.LawyerId == lawyerId && x.Status == CaseStatus.Active, ct);
        if (matter is null) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.RequiredDocuments) || input.DueAtUtc <= DateTime.UtcNow)
        { TempData["Error"] = "Provide a title, required-document list and future due date."; return RedirectToAction(nameof(Create), new { caseId = input.CaseId }); }
        input.ClientId = matter.ClientId; input.RequestedByAttorneyId = lawyerId; input.Title = input.Title.Trim();
        input.RequiredDocuments = input.RequiredDocuments.Trim(); input.Instructions = input.Instructions?.Trim() ?? "";
        db.CaseDocumentRequests.Add(input); await db.SaveChangesAsync(ct);
        var clientUser = await db.Users.SingleOrDefaultAsync(x => x.Email == matter.Client.Email, ct);
        if (clientUser is not null)
            await notifications.QueueAsync(clientUser.Id, "CaseDocuments", $"Documents requested for {matter.CaseNumber}", $"{input.Title}. Due {input.DueAtUtc:dd MMM yyyy}.", $"/CaseDocumentRequest/Respond/{input.Id}", $"case-doc-request-{input.Id}", ct);
        await db.SaveChangesAsync(ct); TempData["Success"] = "The client was notified with a direct upload link.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var request = await Query().SingleOrDefaultAsync(x => x.Id == id, ct); if (request is null) return NotFound();
        if (!await CanAccess(request, ct)) return Forbid(); return View(request);
    }

    [RequireSessionRole("Client")]
    public async Task<IActionResult> Respond(int id, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        var request = await Query().SingleOrDefaultAsync(x => x.Id == id && x.ClientId == client.Id, ct); return request is null ? NotFound() : View(request);
    }

    [HttpPost, RequireSessionRole("Client"), RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload(int id, List<IFormFile> files, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        var request = await db.CaseDocumentRequests.Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == id && x.ClientId == client.Id, ct);
        if (request is null) return NotFound();
        if (files.Count == 0 || files.Any(x => x.Length is <= 0 or > 20 * 1024 * 1024)) { TempData["Error"] = "Select files up to 20 MB each."; return RedirectToAction(nameof(Respond), new { id }); }
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".txt" };
        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName); if (!allowed.Contains(extension)) { TempData["Error"] = "Only PDF, Word, image and text documents are accepted."; return RedirectToAction(nameof(Respond), new { id }); }
            var stored = await storage.StoreAsync(request.CaseId, file, ct);
            db.Documents.Add(new Document { FileName = stored.OriginalFileName, FilePath = stored.RelativePath, FileSize = $"{stored.SizeBytes / 1024d:N0} KB", FileType = stored.ContentType, CaseId = request.CaseId, ClientId = request.ClientId, UploadedById = HttpContext.Session.GetInt32("UserId"), Description = $"Client response to: {request.Title}", Category = DocumentCategory.ClientDocuments, UploadedAt = DateTime.UtcNow, VersionNumber = 1, CaseDocumentRequestId = request.Id });
        }
        request.Status = CaseDocumentRequestStatus.Submitted; request.SubmittedAtUtc = DateTime.UtcNow; request.ReviewFeedback = null;
        await notifications.QueueAsync(request.RequestedByAttorneyId, "CaseDocuments", $"Client uploaded documents for {request.Case.CaseNumber}", $"Review the response to {request.Title}.", $"/CaseDocumentRequest/Details/{request.Id}", $"case-doc-submitted-{request.Id}-{request.SubmittedAtUtc:O}", ct);
        await db.SaveChangesAsync(ct); TempData["Success"] = "Documents uploaded and your lawyer was notified."; return RedirectToAction(nameof(Respond), new { id });
    }

    [HttpPost, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Review(int id, bool accepted, string feedback, CancellationToken ct)
    {
        var lawyerId = HttpContext.Session.GetInt32("UserId")!.Value;
        var request = await Query().SingleOrDefaultAsync(x => x.Id == id && x.RequestedByAttorneyId == lawyerId, ct); if (request is null) return Forbid();
        if (!accepted && string.IsNullOrWhiteSpace(feedback)) { TempData["Error"] = "Explain what is missing."; return RedirectToAction(nameof(Details), new { id }); }
        request.Status = accepted ? CaseDocumentRequestStatus.Accepted : CaseDocumentRequestStatus.MoreInformationRequired; request.ReviewFeedback = feedback?.Trim(); request.ReviewedAtUtc = DateTime.UtcNow;
        var clientUser = await db.Users.SingleOrDefaultAsync(x => x.Email == request.Client.Email, ct);
        if (clientUser is not null) await notifications.QueueAsync(clientUser.Id, "CaseDocuments", accepted ? "Documents accepted" : "More documents required", accepted ? $"Your documents for {request.Case.CaseNumber} were accepted." : request.ReviewFeedback!, $"/CaseDocumentRequest/Respond/{request.Id}", $"case-doc-review-{request.Id}-{request.ReviewedAtUtc:O}", ct);
        await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Details), new { id });
    }

    private IQueryable<CaseDocumentRequest> Query() => db.CaseDocumentRequests.Include(x => x.Case).Include(x => x.Client).Include(x => x.RequestedByAttorney).Include(x => x.Documents);
    private async Task<bool> CanAccess(CaseDocumentRequest request, CancellationToken ct)
    {
        var role = HttpContext.Session.GetString("UserRole"); var id = HttpContext.Session.GetInt32("UserId");
        if (role is "Admin" or "Director" || id == request.RequestedByAttorneyId) return true;
        var client = await currentClient.GetAsync(ct); return client?.Id == request.ClientId;
    }
}
