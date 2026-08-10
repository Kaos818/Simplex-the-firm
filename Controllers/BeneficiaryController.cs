using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services.CurrentUser;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Security;
using SimplexLawFirm.Services.Storage;
using SimplexLawFirm.Services.Verification;
using System.Security.Cryptography;

namespace SimplexLawFirm.Controllers;

[RequireSessionRole("Client")]
public class BeneficiaryController(ApplicationDbContext db, ICurrentClientService currentClient, IEmailService email, IConfiguration config,
    ISecureFileStorage storage, ILocalVerificationClient verification) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        var beneficiaries = await db.Beneficiaries.Where(x => x.BenefactorClientId == client.Id).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        var ids = beneficiaries.Select(x => x.Id).ToArray();
        var invitations = await db.EmailOutboxMessages
            .Where(x => x.DeduplicationKey.StartsWith("beneficiary-invite:"))
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        ViewBag.DeliveryStatuses = beneficiaries.ToDictionary(
            x => x.Id,
            x => invitations.FirstOrDefault(m => m.DeduplicationKey.StartsWith($"beneficiary-invite:{x.Id}:"))?.Status);
        return View(beneficiaries);
    }
    public IActionResult Create() => View(new Beneficiary());

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        var beneficiary = await db.Beneficiaries.SingleOrDefaultAsync(x => x.Id == id && x.BenefactorClientId == client.Id, ct);
        if (beneficiary is null) return NotFound();
        if (beneficiary.Status != BeneficiaryStatus.Draft) return BadRequest("Only draft beneficiaries can be edited.");
        return View(beneficiary);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Beneficiary input, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        var beneficiary = await db.Beneficiaries.SingleOrDefaultAsync(x => x.Id == id && x.BenefactorClientId == client.Id, ct);
        if (beneficiary is null) return NotFound();
        if (beneficiary.Status != BeneficiaryStatus.Draft) return BadRequest("Only draft beneficiaries can be edited.");
        RemoveServerOwnedValidation();
        if (input.AccessEligibleFromUtc is not null && input.AccessEligibleUntilUtc is not null && input.AccessEligibleUntilUtc <= input.AccessEligibleFromUtc)
            ModelState.AddModelError(nameof(input.AccessEligibleUntilUtc), "The access end date must be after the access start date.");
        if (!ModelState.IsValid) { input.Id = id; return View(input); }
        beneficiary.FirstName = input.FirstName.Trim(); beneficiary.LastName = input.LastName.Trim();
        beneficiary.Email = input.Email.Trim(); beneficiary.Phone = input.Phone.Trim();
        beneficiary.IdentificationNumber = input.IdentificationNumber.Trim(); beneficiary.DateOfBirth = input.DateOfBirth;
        beneficiary.RelationshipToBenefactor = input.RelationshipToBenefactor.Trim();
        beneficiary.AssetAccessTerms = input.AssetAccessTerms.Trim(); beneficiary.PermittedAssetPurposes = input.PermittedAssetPurposes.Trim();
        beneficiary.EntitlementDescription = input.EntitlementDescription.Trim(); beneficiary.AccessEligibleFromUtc = input.AccessEligibleFromUtc;
        beneficiary.EntitlementAmountLimit = input.EntitlementAmountLimit;
        beneficiary.AccessEligibleUntilUtc = input.AccessEligibleUntilUtc;
        db.AuditEntries.Add(new() { ActorUserId = HttpContext.Session.GetInt32("UserId"), EntityType = "Beneficiary", EntityId = id.ToString(), Action = "Draft beneficiary edited" });
        await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Beneficiary model, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        RemoveServerOwnedValidation();
        if (model.AccessEligibleFromUtc is not null && model.AccessEligibleUntilUtc is not null && model.AccessEligibleUntilUtc <= model.AccessEligibleFromUtc)
            ModelState.AddModelError(nameof(model.AccessEligibleUntilUtc), "The access end date must be after the access start date.");
        if (!ModelState.IsValid) return View(model);
        model.Id = 0; model.BenefactorClientId = client.Id; model.Status = BeneficiaryStatus.Draft; model.CreatedAtUtc = DateTime.UtcNow; model.ReviewedByUserId = null;
        model.AssetAccessTerms = model.AssetAccessTerms.Trim(); model.EntitlementDescription = model.EntitlementDescription.Trim();
        model.PermittedAssetPurposes = model.PermittedAssetPurposes.Trim();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.Beneficiaries.Add(model); await db.SaveChangesAsync(ct);
        db.AuditEntries.Add(new() { ActorUserId = HttpContext.Session.GetInt32("UserId"), EntityType = "Beneficiary", EntityId = model.Id.ToString(), Action = "Beneficiary created" });
        var requirements = await db.BeneficiaryDocumentRequirements.Where(x => x.IsActive).ToListAsync(ct);
        db.BeneficiaryRequirementAssignments.AddRange(requirements.Select(x => new BeneficiaryRequirementAssignment { BeneficiaryId = model.Id, RequirementId = x.Id, IsRequired = x.IsRequired }));
        var token = SecureToken.Create();
        var portalPassword = GeneratePortalPassword();
        model.PortalPasswordHash = BCrypt.Net.BCrypt.HashPassword(portalPassword);
        model.PortalPasswordSetAtUtc = DateTime.UtcNow;
        model.PortalAccessEnabled = true;
        db.BeneficiaryInvitations.Add(new() { BeneficiaryId = model.Id, TokenHash = token.Hash, CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(72), CreatedByUserId = HttpContext.Session.GetInt32("UserId")!.Value });
        model.Status = BeneficiaryStatus.InvitationSent;
        var url = $"{config["Email:PublicBaseUrl"]?.TrimEnd('/')}/BeneficiaryPortal/Welcome?token={Uri.EscapeDataString(token.Raw)}";
        await QueueBeneficiaryInvitationEmailAsync(model, url, portalPassword, token.Hash, ct);
        db.AuditEntries.Add(new() { ActorUserId = HttpContext.Session.GetInt32("UserId"), EntityType = "Beneficiary", EntityId = model.Id.ToString(), Action = "Invitation sent" });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return RedirectToAction(nameof(Index));
    }

    private void RemoveServerOwnedValidation()
    {
        ModelState.Remove(nameof(Beneficiary.BenefactorClient));
        ModelState.Remove(nameof(Beneficiary.ReviewedByUser));
        ModelState.Remove(nameof(Beneficiary.RequirementAssignments));
        ModelState.Remove(nameof(Beneficiary.RowVersion));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(int id, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        var beneficiary = await db.Beneficiaries.SingleOrDefaultAsync(x => x.Id == id && x.BenefactorClientId == client.Id, ct); if (beneficiary is null) return NotFound();
        var old = await db.BeneficiaryInvitations.Where(x => x.BeneficiaryId == id && x.UsedAtUtc == null && x.RevokedAtUtc == null).ToListAsync(ct);
        old.ForEach(x => x.RevokedAtUtc = DateTime.UtcNow);
        var token = SecureToken.Create();
        var portalPassword = GeneratePortalPassword();
        beneficiary.PortalPasswordHash = BCrypt.Net.BCrypt.HashPassword(portalPassword);
        beneficiary.PortalPasswordSetAtUtc = DateTime.UtcNow;
        beneficiary.PortalAccessEnabled = true;
        db.BeneficiaryInvitations.Add(new() { BeneficiaryId = id, TokenHash = token.Hash, CreatedAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddHours(72), CreatedByUserId = HttpContext.Session.GetInt32("UserId")!.Value });
        beneficiary.Status = BeneficiaryStatus.InvitationSent;
        db.AuditEntries.Add(new() { ActorUserId = HttpContext.Session.GetInt32("UserId"), EntityType = "Beneficiary", EntityId = id.ToString(), Action = old.Count == 0 ? "Invitation sent" : "Invitation renewed" });
        var url = $"{config["Email:PublicBaseUrl"]?.TrimEnd('/')}/BeneficiaryPortal/Welcome?token={Uri.EscapeDataString(token.Raw)}";
        await QueueBeneficiaryInvitationEmailAsync(beneficiary, url, portalPassword, token.Hash, ct);
        await db.SaveChangesAsync(ct);
        TempData["DeliveryNotice"] = $"Invitation queued for {beneficiary.Email}. Delivery status will update below after Azure confirms it.";
        return RedirectToAction(nameof(Index));
    }

    private Task QueueBeneficiaryInvitationEmailAsync(Beneficiary beneficiary, string url, string portalPassword, string tokenHash, CancellationToken ct)
    {
        var recipientName = System.Net.WebUtility.HtmlEncode(beneficiary.FirstName);
        var encodedUrl = System.Net.WebUtility.HtmlEncode(url);
        var encodedEmail = System.Net.WebUtility.HtmlEncode(beneficiary.Email);
        var encodedPassword = System.Net.WebUtility.HtmlEncode(portalPassword);
        var html = $"""
            <div style="margin:0;padding:32px 16px;background:#eef5f3;font-family:Arial,Helvetica,sans-serif;color:#16343a">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width:620px;margin:0 auto;background:#ffffff;border-radius:18px;overflow:hidden;box-shadow:0 8px 30px rgba(10,44,52,.12)">
                <tr><td style="padding:28px 34px;background:#0b2b37;color:#ffffff"><div style="font-size:22px;font-weight:800;letter-spacing:-.4px">Simplex <span style="color:#b9f4df">Attorneys</span></div><div style="margin-top:7px;font-size:12px;letter-spacing:1.3px;text-transform:uppercase;color:#b9f4df">Secure beneficiary portal</div></td></tr>
                <tr><td style="padding:34px"><h1 style="margin:0 0 16px;font-size:27px;line-height:1.2;color:#16343a">You have been named as a beneficiary</h1><p style="margin:0 0 16px;font-size:16px;line-height:1.65">Hello {recipientName},</p><p style="margin:0 0 24px;font-size:16px;line-height:1.65;color:#486066">Your secure portal lets you review the recorded assets and terms, provide the required documents, and complete identity verification for an access request.</p><table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr><td style="border-radius:10px;background:#0f766e"><a href="{encodedUrl}" style="display:inline-block;padding:14px 22px;color:#ffffff;text-decoration:none;font-size:16px;font-weight:700">Open your beneficiary portal&nbsp; →</a></td></tr></table><p style="margin:24px 0 0;font-size:13px;line-height:1.55;color:#6c7f82">For your security, this personal link expires in 72 hours and can be used once. Identity verification does not automatically approve or release assets.</p></td></tr>
                <tr><td style="padding:20px 34px;background:#f7faf9;border-top:1px solid #e1ece8;font-size:12px;line-height:1.55;color:#6c7f82">If you did not expect this invitation, please contact Simplex Attorneys before opening the link.</td></tr>
              </table>
            </div>
            """;
        html = html.Replace("<table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\"><tr><td style=\"border-radius:10px;background:#0f766e\">",
            $"<div style=\"padding:18px;margin:0 0 24px;border:1px solid #cfe2dc;border-radius:12px;background:#f3faf7\"><strong>Portal email:</strong> {encodedEmail}<br/><strong>Unique password:</strong> <code style=\"padding:4px 7px;background:#e1eee9;border-radius:6px\">{encodedPassword}</code></div><table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\"><tr><td style=\"border-radius:10px;background:#0f766e\">");
        var text = $"Hello {beneficiary.FirstName}, you have been named as a beneficiary. Portal email: {beneficiary.Email}. Unique password: {portalPassword}. Open your setup link: {url}. This link expires in 72 hours and can be used once; afterwards sign in with your email and unique password. Identity verification does not automatically approve or release assets.";
        return email.QueueAsync(beneficiary.Email, "Your Simplex beneficiary portal invitation", html, text,
            $"beneficiary-invite:{beneficiary.Id}:{tokenHash}", ct);
    }

    internal static string GeneratePortalPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%";
        const string all = upper + lower + digits + symbols;
        var characters = new char[16];
        characters[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        characters[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        characters[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        characters[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
        for (var index = 4; index < characters.Length; index++) characters[index] = all[RandomNumberGenerator.GetInt32(all.Length)];
        RandomNumberGenerator.Shuffle(characters);
        return new string(characters);
    }
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        var model = await db.Beneficiaries.Include(x => x.RequirementAssignments).ThenInclude(x => x.Requirement).SingleOrDefaultAsync(x => x.Id == id && x.BenefactorClientId == client.Id, ct);
        if (model is null) return NotFound();
        ViewBag.LatestDocuments = await db.BeneficiaryDocuments.Where(x => x.BeneficiaryId == id).GroupBy(x => x.RequirementId)
            .Select(x => x.OrderByDescending(d => d.UploadedAtUtc).First()).ToDictionaryAsync(x => x.RequirementId, ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, string reason, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        if (string.IsNullOrWhiteSpace(reason)) return BadRequest("A reason is required.");
        var beneficiary = await db.Beneficiaries.SingleOrDefaultAsync(x => x.Id == id && x.BenefactorClientId == client.Id, ct);
        if (beneficiary is null) return NotFound();
        beneficiary.Status = BeneficiaryStatus.Suspended; beneficiary.ManualReviewReason = reason.Trim();
        beneficiary.PortalAccessEnabled = false;
        beneficiary.PortalPasswordHash = null;
        var invitations = await db.BeneficiaryInvitations.Where(x => x.BeneficiaryId == id && x.UsedAtUtc == null && x.RevokedAtUtc == null).ToListAsync(ct);
        invitations.ForEach(x => x.RevokedAtUtc = DateTime.UtcNow);
        var faceSessions = await db.FacialVerificationSessions.Where(x => x.BeneficiaryId == id && x.CompletedAtUtc == null).ToListAsync(ct);
        faceSessions.ForEach(x => { x.Status = FacialVerificationStatus.Cancelled; x.CompletedAtUtc = DateTime.UtcNow; });
        db.AuditEntries.Add(new() { ActorUserId = HttpContext.Session.GetInt32("UserId"), EntityType = "Beneficiary", EntityId = id.ToString(), Action = "Beneficiary deactivated" });
        var encodedReason = System.Net.WebUtility.HtmlEncode(reason.Trim());
        var text = $"Your beneficiary appointment and portal access have been terminated. Reason: {reason.Trim()}. Your password and outstanding invitations have been revoked, and you can no longer view entitlements or request assets.";
        var html = $"<div style=\"font-family:Arial;background:#eef5f3;padding:30px\"><div style=\"max-width:620px;margin:auto;background:white;border-radius:16px;overflow:hidden\"><div style=\"background:#0b2b37;color:white;padding:24px 30px;font-size:22px;font-weight:bold\">Simplex Attorneys</div><div style=\"padding:30px\"><h1 style=\"color:#16343a\">Beneficiary access terminated</h1><p>Your beneficiary appointment and portal access have been terminated.</p><p><strong>Reason:</strong> {encodedReason}</p><div style=\"padding:15px;background:#fff4f2;border-left:4px solid #b42318\">Your password, open invitations and future portal access are now revoked.</div><p style=\"margin-top:22px\">Contact the benefactor or Simplex Attorneys if you believe this notice was sent in error.</p></div></div></div>";
        await email.QueueAsync(beneficiary.Email, "Your beneficiary access has been terminated", html, text, $"beneficiary-deactivated:{beneficiary.Id}:{beneficiary.Status}", ct);
        await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> DownloadDocument(long documentId, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        var document = await db.BeneficiaryDocuments.Include(x => x.Beneficiary)
            .SingleOrDefaultAsync(x => x.Id == documentId && x.Beneficiary.BenefactorClientId == client.Id, ct);
        if (document is null) return NotFound();
        var stream = await storage.OpenReadAsync(document.RelativeStoragePath, ct);
        return File(stream, document.ContentType, document.OriginalFileName, enableRangeProcessing: false);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(LocalSecureFileStorage.MaximumBytes + 1024)]
    public async Task<IActionResult> UploadDocument(int id, int requirementId, IFormFile file, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        var beneficiary = await db.Beneficiaries.SingleOrDefaultAsync(x => x.Id == id && x.BenefactorClientId == client.Id, ct);
        var assignment = await db.BeneficiaryRequirementAssignments.Include(x => x.Requirement)
            .SingleOrDefaultAsync(x => x.BeneficiaryId == id && x.RequirementId == requirementId, ct);
        if (beneficiary is null || assignment is null) return NotFound();
        try
        {
            var saved = await storage.StoreAsync(id, file, ct);
            var document = new BeneficiaryDocument { BeneficiaryId=id,RequirementId=requirementId,OriginalFileName=saved.OriginalFileName,
                StoredFileName=saved.StoredFileName,RelativeStoragePath=saved.RelativePath,ContentType=saved.ContentType,SizeBytes=saved.SizeBytes,
                Sha256Hash=saved.Sha256Hash,PreScreenStatus=DocumentPreScreenStatus.Processing };
            db.BeneficiaryDocuments.Add(document); await db.SaveChangesAsync(ct);
            await using var stream = await storage.OpenReadAsync(saved.RelativePath, ct);
            try
            {
                var json = await verification.AnalyseDocumentAsync(stream,saved.OriginalFileName,assignment.Requirement.Code,
                    assignment.Requirement.RequiresCertifiedCopy,assignment.Requirement.RequiresExpiryCheck,ct);
                using var parsed=System.Text.Json.JsonDocument.Parse(json);var root=parsed.RootElement;var decision=root.GetProperty("decision").GetString();
                document.PreScreenStatus=decision switch{"PASSED"=>DocumentPreScreenStatus.Passed,"MANUAL_REVIEW"=>DocumentPreScreenStatus.ManualReviewRequired,
                    "RESUBMIT"=>DocumentPreScreenStatus.ResubmissionRequired,_=>DocumentPreScreenStatus.FailedTechnicalProcessing};
                document.ReasonCode=root.TryGetProperty("reason_code",out var code)&&code.ValueKind==System.Text.Json.JsonValueKind.String?code.GetString():null;
                document.UserFacingReason=root.TryGetProperty("user_facing_reason",out var reason)?reason.GetString():null;
                document.QualityScore=root.TryGetProperty("quality_score",out var quality)&&quality.ValueKind==System.Text.Json.JsonValueKind.Number?quality.GetDecimal():null;
                document.OcrConfidence=root.TryGetProperty("ocr_confidence",out var confidence)&&confidence.ValueKind==System.Text.Json.JsonValueKind.Number?confidence.GetDecimal():null;
                document.CertificationWordingDetected=root.TryGetProperty("certification_wording_detected",out var wording)?wording.GetBoolean():null;
                document.CertificationStampDetected=root.TryGetProperty("stamp_detected",out var stamp)?stamp.GetBoolean():null;
                document.SignatureDetected=root.TryGetProperty("signature_detected",out var signature)?signature.GetBoolean():null;
            }
            catch { document.PreScreenStatus=DocumentPreScreenStatus.FailedTechnicalProcessing;document.UserFacingReason="The verification service is temporarily unavailable. Please retry safely."; }
            document.AnalysedAtUtc=DateTime.UtcNow;
            db.AuditEntries.Add(new(){ActorUserId=HttpContext.Session.GetInt32("UserId"),EntityType="BeneficiaryDocument",EntityId=document.Id.ToString(),Action="Document uploaded",
                SafeMetadataJson=System.Text.Json.JsonSerializer.Serialize(new{status=document.PreScreenStatus.ToString(),document.ReasonCode})});
            await db.SaveChangesAsync(ct); TempData["DocumentResult"]=document.UserFacingReason;
            return RedirectToAction(nameof(Details),new{id});
        }
        catch(InvalidDataException ex){TempData["DocumentResult"]=ex.Message;return RedirectToAction(nameof(Details),new{id});}
    }
}
