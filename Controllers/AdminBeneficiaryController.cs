using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services.Storage;

namespace SimplexLawFirm.Controllers;

[RequireSessionRole("Admin")]
public class AdminBeneficiaryController(
    ApplicationDbContext db,
    IEmailService email,
    INotificationService notifications,
    ISecureFileStorage? storage = null,
    IConfiguration? configuration = null) : Controller
{
    public async Task<IActionResult> Index(BeneficiaryStatus? status, CancellationToken ct)
    {
        var query = db.Beneficiaries.Include(x => x.BenefactorClient).AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        return View(await query.OrderByDescending(x => x.SubmittedAtUtc).ToListAsync(ct));
    }
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var model = await db.Beneficiaries.Include(x => x.BenefactorClient).Include(x => x.RequirementAssignments).ThenInclude(x => x.Requirement).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (model is null) return NotFound();
        ViewBag.LatestDocuments = await db.BeneficiaryDocuments.Where(x => x.BeneficiaryId == id).GroupBy(x => x.RequirementId)
            .Select(x => x.OrderByDescending(d => d.UploadedAtUtc).First()).ToDictionaryAsync(x => x.RequirementId, ct);
        ViewBag.LatestFace = await db.FacialVerificationSessions.Where(x => x.BeneficiaryId == id).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        ViewBag.Audit = await db.AuditEntries.Where(x => x.EntityType == "Beneficiary" && x.EntityId == id.ToString() || x.EntityType == "FacialVerificationSession" && db.FacialVerificationSessions.Where(s => s.BeneficiaryId == id).Select(s => s.Id.ToString()).Contains(x.EntityId))
            .OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(ct);
        return View(model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? overrideReason, CancellationToken ct)
    {
        var b = await db.Beneficiaries.Include(x => x.BenefactorClient).Include(x => x.RequirementAssignments).SingleOrDefaultAsync(x => x.Id == id, ct); if (b is null) return NotFound();
        if (b.Status != BeneficiaryStatus.UnderAdminReview) return BadRequest("The application must be submitted for Director review before approval.");
        var required = b.RequirementAssignments.Where(x => x.IsRequired).Select(x => x.RequirementId).ToList();
        foreach (var requirement in required)
        {
            var latest = await db.BeneficiaryDocuments.Where(x => x.BeneficiaryId == id && x.RequirementId == requirement).OrderByDescending(x => x.UploadedAtUtc).FirstOrDefaultAsync(ct);
            if (latest is null || latest.PreScreenStatus is not (DocumentPreScreenStatus.Passed or DocumentPreScreenStatus.ManualReviewRequired))
                return BadRequest("All required documents must pass pre-screening or Director review.");
        }
        var face = await db.FacialVerificationSessions.Where(x => x.BeneficiaryId == id).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (face?.Status == FacialVerificationStatus.ManualReviewRequired && string.IsNullOrWhiteSpace(overrideReason)) return BadRequest("A Director reason is required for a facial verification override.");
        if (face?.Status is not (FacialVerificationStatus.Verified or FacialVerificationStatus.ManualReviewRequired)) return BadRequest("Facial verification has not passed.");
        b.Status = BeneficiaryStatus.Approved; b.PortalAccessEnabled = true; b.ReviewedAtUtc = DateTime.UtcNow; b.ReviewedByUserId = HttpContext.Session.GetInt32("UserId"); b.ManualReviewReason = overrideReason;
        db.AuditEntries.Add(new() { ActorUserId = b.ReviewedByUserId, EntityType = "Beneficiary", EntityId = id.ToString(), Action = "Director approved",
            SafeMetadataJson = overrideReason is null ? null : System.Text.Json.JsonSerializer.Serialize(new { manualOverride = true }) });
        await QueueDecisionNotificationsAsync(b, approved: true, null, ct);
        await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestDocument(int id, int requirementId, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason)) return BadRequest("A user-facing reason is required.");
        var b = await db.Beneficiaries.Include(x => x.BenefactorClient).Include(x => x.RequirementAssignments).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return NotFound();
        if (!b.RequirementAssignments.Any(x => x.RequirementId == requirementId)) return BadRequest("The document requirement is not assigned to this beneficiary.");
        b.Status = BeneficiaryStatus.DocumentsRequireResubmission; b.ManualReviewReason = reason.Trim();
        db.AuditEntries.Add(new() { ActorUserId = HttpContext.Session.GetInt32("UserId"), EntityType = "Beneficiary", EntityId = id.ToString(), Action = "Document resubmission requested",
            SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(new { requirementId }) });
        await QueueReviewRequestAsync(b, "New beneficiary document required", reason.Trim(), ct);
        await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestFaceCapture(int id, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason)) return BadRequest("A user-facing reason is required.");
        var b = await db.Beneficiaries.Include(x => x.BenefactorClient).SingleOrDefaultAsync(x => x.Id == id, ct); if (b is null) return NotFound();
        var open = await db.FacialVerificationSessions.Where(x => x.BeneficiaryId == id && x.CompletedAtUtc == null).ToListAsync(ct);
        open.ForEach(x => { x.Status = FacialVerificationStatus.Cancelled; x.CompletedAtUtc = DateTime.UtcNow; });
        b.Status = BeneficiaryStatus.AwaitingFacialVerification; b.ManualReviewReason = reason.Trim();
        db.AuditEntries.Add(new() { ActorUserId = HttpContext.Session.GetInt32("UserId"), EntityType = "Beneficiary", EntityId = id.ToString(), Action = "New facial capture requested" });
        await QueueReviewRequestAsync(b, "Please repeat live identity verification", reason.Trim(), ct);
        await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Scrutinize(int id, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason)) return BadRequest("A user-facing reason is required.");
        var b = await db.Beneficiaries.Include(x => x.BenefactorClient).SingleOrDefaultAsync(x => x.Id == id, ct); if (b is null) return NotFound();
        b.Status = BeneficiaryStatus.UnderAdminReview; b.ManualReviewReason = reason.Trim();
        db.AuditEntries.Add(new() { ActorUserId = HttpContext.Session.GetInt32("UserId"), EntityType = "Beneficiary", EntityId = id.ToString(), Action = "Further scrutiny requested" });
        await QueueReviewRequestAsync(b, "Beneficiary application requires further review", reason.Trim(), ct);
        await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> DownloadDocument(long documentId, CancellationToken ct)
    {
        var document = await db.BeneficiaryDocuments.SingleOrDefaultAsync(x => x.Id == documentId, ct);
        if (document is null) return NotFound();
        if (storage is null) return StatusCode(503, "Secure document storage is unavailable.");
        var stream = await storage.OpenReadAsync(document.RelativeStoragePath, ct);
        return File(stream, document.ContentType, document.OriginalFileName, enableRangeProcessing: false);
    }

    private async Task QueueReviewRequestAsync(Beneficiary beneficiary, string subject, string reason, CancellationToken ct)
    {
        var text = $"{subject}. Reason: {reason}";
        await email.QueueAsync(beneficiary.Email, subject, $"<p>{System.Net.WebUtility.HtmlEncode(text)}</p>", text, $"beneficiary-review-request:{beneficiary.Id}:{subject}:{beneficiary.Status}", ct);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == beneficiary.BenefactorClient.Email, ct);
        if (user is not null) await notifications.QueueAsync(user.Id, "BeneficiaryReview", subject, text, $"/Beneficiary/Details/{beneficiary.Id}", $"beneficiary-review-request:{beneficiary.Id}:{subject}", ct);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason)) return BadRequest("A reason is required.");
        var b = await db.Beneficiaries.Include(x => x.BenefactorClient).SingleOrDefaultAsync(x => x.Id == id, ct); if (b is null) return NotFound();
        b.Status = BeneficiaryStatus.Rejected; b.RejectionReason = reason.Trim(); b.ReviewedAtUtc = DateTime.UtcNow; b.ReviewedByUserId = HttpContext.Session.GetInt32("UserId");
        db.AuditEntries.Add(new() { ActorUserId = b.ReviewedByUserId, EntityType = "Beneficiary", EntityId = id.ToString(), Action = "Director rejected" });
        await QueueDecisionNotificationsAsync(b, approved: false, b.RejectionReason, ct);
        await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Details), new { id });
    }

    private async Task QueueDecisionNotificationsAsync(Beneficiary beneficiary, bool approved, string? reason, CancellationToken ct)
    {
        var beneficiaryName = $"{beneficiary.FirstName} {beneficiary.LastName}".Trim();
        var encodedName = System.Net.WebUtility.HtmlEncode(beneficiaryName);
        var encodedReason = System.Net.WebUtility.HtmlEncode(reason);
        var subject = approved ? "Beneficiary verification approved" : "Beneficiary verification needs attention";
        var text = approved
            ? $"Hello {beneficiaryName}, your beneficiary verification has been approved. Approval confirms identity and documents only; asset access remains governed by the fund terms shown in your portal."
            : $"Hello {beneficiaryName}, your beneficiary verification was not approved. Reason: {reason}. Please contact your benefactor or the law firm before submitting corrected information.";
        var portalUrl = $"{configuration?["Email:PublicBaseUrl"]?.TrimEnd('/') ?? ""}/BeneficiaryPortal/Login";
        var encodedPortalUrl = System.Net.WebUtility.HtmlEncode(portalUrl);
        var html = approved
            ? $"<div style=\"font-family:Arial;background:#eef5f3;padding:30px\"><div style=\"max-width:620px;margin:auto;background:white;border-radius:16px;overflow:hidden\"><div style=\"background:#0b2b37;color:white;padding:24px 30px;font-size:22px;font-weight:bold\">Simplex Attorneys</div><div style=\"padding:30px\"><h1 style=\"color:#16343a\">Verification approved</h1><p>Hello {encodedName},</p><p>Your beneficiary identity and documentation verification has been approved.</p><p>You will only see the entitlement, purposes and limits recorded specifically for you. The benefactor’s other assets and total trust balances remain private.</p><p><a href=\"{encodedPortalUrl}\" style=\"display:inline-block;background:#0f766e;color:white;text-decoration:none;padding:13px 20px;border-radius:9px;font-weight:bold\">Open beneficiary portal</a></p><p style=\"color:#617671;font-size:13px\">Approval does not automatically release funds; every request remains subject to your recorded terms and final authorisation.</p></div></div></div>"
            : $"<p>Hello {encodedName},</p><p>Your beneficiary verification was not approved.</p><p><strong>Reason:</strong> {encodedReason}</p><p>Please contact your benefactor or the law firm before submitting corrected information.</p>";
        await email.QueueAsync(beneficiary.Email, subject, html, text, $"beneficiary-decision:{beneficiary.Id}:{beneficiary.Status}", ct);

        var benefactorUser = await db.Users.SingleOrDefaultAsync(
            x => x.Email == beneficiary.BenefactorClient.Email, ct);
        if (benefactorUser is null) return;

        var message = approved
            ? $"{beneficiaryName}'s beneficiary verification was approved."
            : $"{beneficiaryName}'s beneficiary verification was rejected. Reason: {reason}";
        await notifications.QueueAsync(
            benefactorUser.Id,
            "BeneficiaryDecision",
            subject,
            message,
            $"/Beneficiary/Details/{beneficiary.Id}",
            $"beneficiary-decision:{beneficiary.Id}:{beneficiary.Status}",
            ct);
        await email.QueueAsync(
            beneficiary.BenefactorClient.Email,
            subject,
            $"<p>{System.Net.WebUtility.HtmlEncode(message)}</p>",
            message,
            $"benefactor-beneficiary-decision:{beneficiary.Id}:{beneficiary.Status}",
            ct);
    }
}
