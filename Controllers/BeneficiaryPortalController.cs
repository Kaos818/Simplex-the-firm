using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services.Security;
using SimplexLawFirm.Services.Storage;
using SimplexLawFirm.Services.Verification;
using System.Security.Cryptography;

namespace SimplexLawFirm.Controllers;

public class BeneficiaryPortalController(ApplicationDbContext db, ISecureFileStorage storage, ILocalVerificationClient verification,
    IReferenceFaceExtractor? referenceExtractor = null, SimplexLawFirm.Services.IOperationalUseCaseService? operational = null) : Controller
{
    private const string PortalKey = "BeneficiaryPortalId";
    [HttpGet]
    public IActionResult Login() => HttpContext.Session.GetInt32(PortalKey) is null ? View() : RedirectToAction(nameof(Assets));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, CancellationToken ct)
    {
        var normalizedEmail = email?.Trim().ToUpperInvariant();
        var beneficiary = string.IsNullOrWhiteSpace(normalizedEmail) ? null : await db.Beneficiaries
            .SingleOrDefaultAsync(x => x.Email.ToUpper() == normalizedEmail, ct);
        if (beneficiary is null || !beneficiary.PortalAccessEnabled || string.IsNullOrWhiteSpace(beneficiary.PortalPasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(password ?? string.Empty, beneficiary.PortalPasswordHash) || beneficiary.Status == BeneficiaryStatus.Suspended)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View();
        }
        HttpContext.Session.SetInt32(PortalKey, beneficiary.Id);
        return RedirectToAction(nameof(Assets));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(PortalKey);
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> Welcome(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) return BadRequest();
        var hash = SecureToken.Hash(token);
        var invite = await db.BeneficiaryInvitations.Include(x => x.Beneficiary).SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (invite is null || invite.ExpiresAtUtc <= DateTime.UtcNow || invite.RevokedAtUtc != null || invite.UsedAtUtc != null || invite.Beneficiary.Status == BeneficiaryStatus.Suspended) return View("InvalidInvitation");
        invite.UsedAtUtc = DateTime.UtcNow;
        if (invite.Beneficiary.Status is BeneficiaryStatus.Draft or BeneficiaryStatus.InvitationSent)
            invite.Beneficiary.Status = BeneficiaryStatus.AwaitingDocuments;
        HttpContext.Session.SetInt32(PortalKey, invite.BeneficiaryId); await db.SaveChangesAsync(ct);
        return View(invite.Beneficiary);
    }
    public async Task<IActionResult> Documents(CancellationToken ct)
    {
        var id = HttpContext.Session.GetInt32(PortalKey); if (id is null) return Unauthorized();
        var model = await db.Beneficiaries.Include(x => x.RequirementAssignments).ThenInclude(x => x.Requirement)
            .SingleOrDefaultAsync(x => x.Id == id && x.PortalAccessEnabled && x.Status != BeneficiaryStatus.Suspended, ct);
        if (model is null) return NotFound();
        ViewBag.LatestDocuments = await db.BeneficiaryDocuments.Where(x => x.BeneficiaryId == id)
            .GroupBy(x => x.RequirementId).Select(x => x.OrderByDescending(d => d.UploadedAtUtc).First()).ToDictionaryAsync(x => x.RequirementId, ct);
        ViewBag.LatestFace = await db.FacialVerificationSessions.Where(x => x.BeneficiaryId == id).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        return View(model);
    }
    public async Task<IActionResult> Assets(CancellationToken ct)
    {
        var id = HttpContext.Session.GetInt32(PortalKey); if (id is null) return Unauthorized();
        var beneficiary = await db.Beneficiaries.SingleOrDefaultAsync(x => x.Id == id && x.PortalAccessEnabled && x.Status != BeneficiaryStatus.Suspended, ct);
        if (beneficiary is null) { HttpContext.Session.Remove(PortalKey); return RedirectToAction(nameof(Login)); }
        return View("Welcome", beneficiary);
    }
    public async Task<IActionResult> BankDetails(CancellationToken ct)
    {
        var id = HttpContext.Session.GetInt32(PortalKey); if (id is null) return Unauthorized();
        var beneficiary = await db.Beneficiaries.SingleOrDefaultAsync(x => x.Id == id && x.Status == BeneficiaryStatus.Approved, ct);
        return beneficiary is null ? Forbid() : View(beneficiary);
    }
    public async Task<IActionResult> TrustRequests(CancellationToken ct)
    {
        var id=HttpContext.Session.GetInt32(PortalKey); if(id is null)return Unauthorized();
        var beneficiary=await db.Beneficiaries.SingleOrDefaultAsync(x=>x.Id==id&&x.PortalAccessEnabled&&x.Status!=BeneficiaryStatus.Suspended,ct);
        if(beneficiary is null){HttpContext.Session.Remove(PortalKey);return RedirectToAction(nameof(Login));}
        var committed=await db.BeneficiaryTrustDisbursementRequests.Where(x=>x.BeneficiaryId==id&&x.Status!=TrustDisbursementStatus.Rejected).SumAsync(x=>(decimal?)x.Amount,ct)??0;
        ViewBag.EntitlementLimit=beneficiary.EntitlementAmountLimit;
        ViewBag.EntitlementRemaining=beneficiary.EntitlementAmountLimit is null?(decimal?)null:Math.Max(0,beneficiary.EntitlementAmountLimit.Value-committed);
        return View(await db.BeneficiaryTrustDisbursementRequests.Where(x=>x.BeneficiaryId==id).OrderByDescending(x=>x.SubmittedAtUtc).ToListAsync(ct));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestTrustDisbursement(string purpose,decimal amount,string reason,CancellationToken ct)
    {
        var id=HttpContext.Session.GetInt32(PortalKey);if(id is null)return Unauthorized();
        try{if(operational is null)throw new InvalidOperationException("Trust request service is unavailable.");await operational.RequestDisbursementAsync(id.Value,purpose,amount,reason,ct);TempData["Success"]="Trust request submitted.";}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(TrustRequests));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BankDetails(string accountHolder, string bankName, string accountNumber, string branchCode, CancellationToken ct)
    {
        var id = HttpContext.Session.GetInt32(PortalKey); if (id is null) return Unauthorized();
        var beneficiary = await db.Beneficiaries.SingleOrDefaultAsync(x => x.Id == id && x.Status == BeneficiaryStatus.Approved, ct);
        if (beneficiary is null) return Forbid();
        if (string.IsNullOrWhiteSpace(accountHolder) || string.IsNullOrWhiteSpace(bankName) || string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(branchCode)) { ModelState.AddModelError(string.Empty, "Complete every bank detail."); return View(beneficiary); }
        beneficiary.BankAccountHolder = accountHolder.Trim(); beneficiary.BankName = bankName.Trim(); beneficiary.BankAccountNumber = accountNumber.Trim(); beneficiary.BankBranchCode = branchCode.Trim(); beneficiary.BankDetailsConfirmedAtUtc = DateTime.UtcNow;
        db.AuditEntries.Add(new() { EntityType = "Beneficiary", EntityId = beneficiary.Id.ToString(), Action = "Beneficiary bank details submitted" }); await db.SaveChangesAsync(ct);
        TempData["Success"] = "Bank details submitted for Director-controlled payout processing."; return RedirectToAction(nameof(Assets));
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(LocalSecureFileStorage.MaximumBytes + 1024)]
    public async Task<IActionResult> Upload(int requirementId, IFormFile file, CancellationToken ct)
    {
        var id = HttpContext.Session.GetInt32(PortalKey); if (id is null) return Unauthorized();
        if (!await db.Beneficiaries.AnyAsync(x => x.Id == id && x.PortalAccessEnabled && x.Status != BeneficiaryStatus.Suspended, ct)) return Forbid();
        var assignment = await db.BeneficiaryRequirementAssignments.Include(x => x.Requirement).SingleOrDefaultAsync(x => x.BeneficiaryId == id && x.RequirementId == requirementId, ct); if (assignment is null) return BadRequest();
        try
        {
            var saved = await storage.StoreAsync(id.Value, file, ct);
            var document = new BeneficiaryDocument { BeneficiaryId = id.Value, RequirementId = requirementId, OriginalFileName = saved.OriginalFileName, StoredFileName = saved.StoredFileName, RelativeStoragePath = saved.RelativePath, ContentType = saved.ContentType, SizeBytes = saved.SizeBytes, Sha256Hash = saved.Sha256Hash, PreScreenStatus = DocumentPreScreenStatus.Processing };
            db.BeneficiaryDocuments.Add(document); await db.SaveChangesAsync(ct);
            await using var stream = await storage.OpenReadAsync(saved.RelativePath, ct);
            try
            {
                var json = await verification.AnalyseDocumentAsync(stream, saved.OriginalFileName, assignment.Requirement.Code, assignment.Requirement.RequiresCertifiedCopy, assignment.Requirement.RequiresExpiryCheck, ct);
                using var parsed = System.Text.Json.JsonDocument.Parse(json);
                var decision = parsed.RootElement.GetProperty("decision").GetString();
                document.PreScreenStatus = decision switch { "PASSED" => DocumentPreScreenStatus.Passed, "MANUAL_REVIEW" => DocumentPreScreenStatus.ManualReviewRequired, "RESUBMIT" => DocumentPreScreenStatus.ResubmissionRequired, _ => DocumentPreScreenStatus.FailedTechnicalProcessing };
                document.ReasonCode = parsed.RootElement.TryGetProperty("reason_code", out var rc) ? rc.GetString() : null;
                document.UserFacingReason = parsed.RootElement.TryGetProperty("user_facing_reason", out var ur) ? ur.GetString() : null;
                document.ExtractedDocumentType = parsed.RootElement.TryGetProperty("document_type", out var type) && type.ValueKind == System.Text.Json.JsonValueKind.String ? type.GetString() : null;
                document.QualityScore = parsed.RootElement.TryGetProperty("quality_score", out var quality) && quality.ValueKind == System.Text.Json.JsonValueKind.Number ? quality.GetDecimal() : null;
                document.OcrConfidence = parsed.RootElement.TryGetProperty("ocr_confidence", out var confidence) && confidence.ValueKind == System.Text.Json.JsonValueKind.Number ? confidence.GetDecimal() : null;
                document.CertificationWordingDetected = parsed.RootElement.TryGetProperty("certification_wording_detected", out var wording) ? wording.GetBoolean() : null;
                document.CertificationStampDetected = parsed.RootElement.TryGetProperty("stamp_detected", out var stamp) ? stamp.GetBoolean() : null;
                document.SignatureDetected = parsed.RootElement.TryGetProperty("signature_detected", out var signature) ? signature.GetBoolean() : null;
                document.TechnicalResultJson = System.Text.Json.JsonSerializer.Serialize(new { decision, document.ReasonCode, document.QualityScore, document.OcrConfidence,
                    document.CertificationWordingDetected, document.CertificationStampDetected, document.SignatureDetected });
            }
            catch
            {
                // A live verification API must never dead-end an applicant. The uploaded file has
                // already passed local type/size/storage checks; route it to the Director instead.
                document.PreScreenStatus = DocumentPreScreenStatus.ManualReviewRequired;
                document.ReasonCode = "LOCAL_MANUAL_REVIEW";
                document.UserFacingReason = "Document received. It will be reviewed by the Director; you may continue to facial verification.";
            }
            document.AnalysedAtUtc = DateTime.UtcNow;
            db.AuditEntries.Add(new() { EntityType = "BeneficiaryDocument", EntityId = document.Id.ToString(), Action = "Document pre-screened",
                SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(new { status = document.PreScreenStatus.ToString(), document.ReasonCode }) });
            var latestStatuses = await db.BeneficiaryRequirementAssignments.Where(x => x.BeneficiaryId == id && x.IsRequired)
                .Select(x => new { x.RequirementId, Status = db.BeneficiaryDocuments.Where(d => d.BeneficiaryId == id && d.RequirementId == x.RequirementId)
                    .OrderByDescending(d => d.UploadedAtUtc).Select(d => (DocumentPreScreenStatus?)d.PreScreenStatus).FirstOrDefault() }).ToListAsync(ct);
            var beneficiary = await db.Beneficiaries.FindAsync([id.Value], ct);
            if (beneficiary is not null)
            {
                var effective = latestStatuses.Select(x => x.RequirementId == requirementId ? (DocumentPreScreenStatus?)document.PreScreenStatus : x.Status).ToList();
                beneficiary.Status = effective.Any(x => x is DocumentPreScreenStatus.ResubmissionRequired or DocumentPreScreenStatus.FailedTechnicalProcessing)
                    ? BeneficiaryStatus.DocumentsRequireResubmission
                    : effective.Count > 0 && effective.All(x => x is DocumentPreScreenStatus.Passed or DocumentPreScreenStatus.ManualReviewRequired)
                        ? BeneficiaryStatus.AwaitingFacialVerification : BeneficiaryStatus.AwaitingDocuments;
            }
            await db.SaveChangesAsync(ct); return View("DocumentResult", document);
        }
        catch (InvalidDataException ex) { ModelState.AddModelError("file", ex.Message); return RedirectToAction(nameof(Documents)); }
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(20 * LocalSecureFileStorage.MaximumBytes)]
    public async Task<IActionResult> UploadAll(List<IFormFile> files, CancellationToken ct)
    {
        var id = HttpContext.Session.GetInt32(PortalKey); if (id is null) return Unauthorized();
        var assignments = await db.BeneficiaryRequirementAssignments.Include(x => x.Requirement).Where(x => x.BeneficiaryId == id).ToListAsync(ct);
        var required = assignments.Where(x => x.IsRequired).OrderBy(x => x.Requirement.DisplayOrder).ToList();
        if (files.Count != 1) { TempData["Error"] = "Select one combined evidence pack (PDF, JPG or PNG)."; return RedirectToAction(nameof(Documents)); }
        var saved = await storage.StoreAsync(id.Value, files[0], ct);
        foreach (var assignment in required)
        {
            db.BeneficiaryDocuments.Add(new BeneficiaryDocument { BeneficiaryId = id.Value, RequirementId = assignment.RequirementId, OriginalFileName = saved.OriginalFileName, StoredFileName = saved.StoredFileName, RelativeStoragePath = saved.RelativePath, ContentType = saved.ContentType, SizeBytes = saved.SizeBytes, Sha256Hash = saved.Sha256Hash, PreScreenStatus = DocumentPreScreenStatus.ManualReviewRequired, ReasonCode = "LOCAL_MANUAL_REVIEW", UserFacingReason = "Document received and queued for Director review.", AnalysedAtUtc = DateTime.UtcNow });
        }
        var beneficiary = await db.Beneficiaries.FindAsync([id.Value]);
        if (beneficiary is not null) beneficiary.Status = BeneficiaryStatus.AwaitingFacialVerification;
        db.AuditEntries.Add(new() { EntityType = "Beneficiary", EntityId = id.Value.ToString(), Action = "All required documents uploaded for manual review" });
        await db.SaveChangesAsync(ct); TempData["Success"] = "Documents received. Continue to facial verification."; return RedirectToAction(nameof(Documents));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Consent(bool consent, CancellationToken ct)
    {
        var id = HttpContext.Session.GetInt32(PortalKey); if (id is null) return Unauthorized();
        if (!consent) return BadRequest("Consent is required. Manual verification is available from the administrator.");
        var beneficiary = await db.Beneficiaries.FindAsync([id.Value], ct);
        if (beneficiary?.Status != BeneficiaryStatus.AwaitingFacialVerification)
            return BadRequest("Complete all required document uploads before live facial verification.");
        var challengePool = new[] { "BLINK", "TURN_LEFT", "TURN_RIGHT", "OPEN_MOUTH" };
        RandomNumberGenerator.Shuffle(challengePool);
        var challenges = challengePool.Take(3).ToArray();
        var session = new FacialVerificationSession { Id = Guid.NewGuid(), BeneficiaryId = id.Value, ChallengeJson = System.Text.Json.JsonSerializer.Serialize(challenges), Status = FacialVerificationStatus.ReadyForCapture, ConsentGranted = true, ConsentGrantedAtUtc = DateTime.UtcNow, ConsentNoticeVersion = "1.0", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(20) };
        db.FacialVerificationSessions.Add(session); db.BiometricConsentRecords.Add(new() { VerificationSessionId = session.Id, BeneficiaryId = id.Value, NoticeVersion = "1.0", NoticeTextHash = SecureToken.Hash("Simplex biometric consent v1.0"), ConsentGranted = true, RecordedAtUtc = DateTime.UtcNow, UserAgent = Request.Headers.UserAgent.ToString()[..Math.Min(500, Request.Headers.UserAgent.ToString().Length)] });
        db.AuditEntries.Add(new() { EntityType = "Beneficiary", EntityId = id.Value.ToString(), Action = "Facial consent recorded" });
        await db.SaveChangesAsync(ct); return RedirectToAction(nameof(FaceCapture), new { sessionId = session.Id });
    }
    public async Task<IActionResult> FaceCapture(Guid sessionId, CancellationToken ct)
    {
        var id = HttpContext.Session.GetInt32(PortalKey); if (id is null) return Unauthorized();
        var session = await db.FacialVerificationSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.BeneficiaryId == id && x.ExpiresAtUtc > DateTime.UtcNow && x.Status == FacialVerificationStatus.ReadyForCapture, ct);
        return session is null ? NotFound() : View(session);
    }

    // The client may submit up to 60 one-megabyte JPEG frames. Leave protocol overhead headroom.
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(61 * 1024 * 1024)]
    public async Task<IActionResult> SubmitFaceCapture(Guid sessionId, List<IFormFile> frames, string timestamps, string stageIndexes, CancellationToken ct)
    {
        var beneficiaryId = HttpContext.Session.GetInt32(PortalKey);
        if (beneficiaryId is null) return Unauthorized();
        var session = await db.FacialVerificationSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.BeneficiaryId == beneficiaryId, ct);
        if (session is null) return NotFound();
        if (!session.ConsentGranted) return BadRequest(new { message = "Biometric consent is required." });
        if (session.ExpiresAtUtc <= DateTime.UtcNow) { session.Status = FacialVerificationStatus.Expired; await db.SaveChangesAsync(ct); return BadRequest(new { message = "This verification session has expired." }); }
        if (session.Status != FacialVerificationStatus.ReadyForCapture) return Conflict(new { message = "This session cannot be submitted again." });
        if (frames.Count is < 20 or > 60 || frames.Any(x => x.Length is <= 0 or > 1_000_000)) return BadRequest(new { message = "Capture 20 to 60 valid camera frames." });

        long[] times; int[] stages; string[] challenges;
        try {
            times = System.Text.Json.JsonSerializer.Deserialize<long[]>(timestamps) ?? [];
            stages = System.Text.Json.JsonSerializer.Deserialize<int[]>(stageIndexes) ?? [];
            challenges = System.Text.Json.JsonSerializer.Deserialize<string[]>(session.ChallengeJson) ?? [];
        } catch (System.Text.Json.JsonException) { return BadRequest(new { message = "Capture metadata is invalid." }); }
        if (times.Length != frames.Count || stages.Length != frames.Count || challenges.Length != 3 ||
            stages.Any(x => x < -1 || x >= challenges.Length) || times.Zip(times.Skip(1), (a, b) => b > a).Any(x => !x))
            return BadRequest(new { message = "Capture metadata is invalid." });

        ReferenceFaceSource? referenceSource = referenceExtractor is null ? null : await referenceExtractor.OpenLatestAsync(beneficiaryId.Value, ct);
        BeneficiaryDocument? idDocument = null;
        if (referenceSource is null)
        {
            idDocument = await db.BeneficiaryDocuments.Include(x => x.Requirement).Where(x => x.BeneficiaryId == beneficiaryId && x.Requirement.Code == "SA_ID" &&
                x.PreScreenStatus == DocumentPreScreenStatus.Passed).OrderByDescending(x => x.UploadedAtUtc).FirstOrDefaultAsync(ct);
            if (idDocument is not null) referenceSource = new ReferenceFaceSource(await storage.OpenReadAsync(idDocument.RelativeStoragePath, ct), idDocument.ContentType);
        }
        if (referenceSource is null) return BadRequest(new { message = "Upload a readable identity document first." });

        db.Entry(session).State = EntityState.Detached;
        var claimed = await db.FacialVerificationSessions.Where(x => x.Id == sessionId && x.BeneficiaryId == beneficiaryId &&
            x.Status == FacialVerificationStatus.ReadyForCapture && x.ExpiresAtUtc > DateTime.UtcNow)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, FacialVerificationStatus.Processing), ct);
        if (claimed != 1) return Conflict(new { message = "This verification session is already being processed or has expired." });
        session = await db.FacialVerificationSessions.SingleAsync(x => x.Id == sessionId, ct);
        try {
            var bytes = new List<byte[]>(frames.Count);
            foreach (var frame in frames) { await using var input = frame.OpenReadStream(); using var memory = new MemoryStream(); await input.CopyToAsync(memory, ct); bytes.Add(memory.ToArray()); }
            await using var reference = referenceSource;
            var json = await verification.VerifyFaceAsync(reference.Content, bytes, session.Id, challenges, times, stages, ct);
            using var result = System.Text.Json.JsonDocument.Parse(json); var root = result.RootElement;
            var decision = root.GetProperty("decision").GetString();
            session.Status = decision switch {
                "VERIFIED" => FacialVerificationStatus.Verified, "MANUAL_REVIEW" => FacialVerificationStatus.ManualReviewRequired,
                "FAILED_LIVENESS" => FacialVerificationStatus.FailedLiveness, "FACE_NOT_MATCHED" => FacialVerificationStatus.FaceNotMatched,
                _ => FacialVerificationStatus.InvalidCapture };
            session.LivenessPassed = root.TryGetProperty("liveness_passed", out var live) && live.GetBoolean();
            session.FaceMatched = root.TryGetProperty("face_matched", out var matched) && matched.GetBoolean();
            if (session.Status == FacialVerificationStatus.Verified && (session.LivenessPassed != true || session.FaceMatched != true))
            {
                session.Status = FacialVerificationStatus.InvalidCapture;
                session.ResultReasonCode = "INCONSISTENT_VERIFICATION_RESULT";
                session.ResultReason = "The verification result was internally inconsistent and was rejected.";
            }
            session.SimilarityScore = root.TryGetProperty("similarity_score", out var similarity) && similarity.ValueKind == System.Text.Json.JsonValueKind.Number ? similarity.GetDecimal() : null;
            session.ValidFrameRatio = root.TryGetProperty("valid_frame_ratio", out var valid) ? valid.GetDecimal() : null;
            session.DuplicateFrameRatio = root.TryGetProperty("duplicate_frame_ratio", out var duplicate) ? duplicate.GetDecimal() : null;
            session.ResultReasonCode ??= root.TryGetProperty("reason_code", out var code) && code.ValueKind == System.Text.Json.JsonValueKind.String ? code.GetString() : null;
            session.ResultReason ??= root.TryGetProperty("reason", out var reason) && reason.ValueKind == System.Text.Json.JsonValueKind.String ? reason.GetString() : null;
            session.CompletedAtUtc = DateTime.UtcNow;
            var beneficiary = await db.Beneficiaries.FindAsync([beneficiaryId.Value], ct);
            if (beneficiary is not null) beneficiary.Status = BeneficiaryStatus.AwaitingFacialVerification;
            db.AuditEntries.Add(new() { EntityType = "FacialVerificationSession", EntityId = session.Id.ToString(), Action = "Facial verification completed",
                SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(new { status = session.Status.ToString(), session.ResultReasonCode }) });
            await db.SaveChangesAsync(ct);
            var canSubmit = session.Status is FacialVerificationStatus.Verified or FacialVerificationStatus.ManualReviewRequired;
            var response = new { status = session.Status.ToString(), canSubmit, message = session.ResultReason ?? (canSubmit ? "Verification completed for Director review." : "Verification did not pass. Please capture a new live session.") };
            return canSubmit ? Ok(response) : UnprocessableEntity(response);
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch {
            session.Status = FacialVerificationStatus.ManualReviewRequired; session.ResultReasonCode = "SERVICE_UNAVAILABLE";
            session.ResultReason = "Automatic verification is temporarily unavailable. The Director must review your application.";
            session.CompletedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct);
            return StatusCode(503, new { message = session.ResultReason });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestManualVerification(string reason, CancellationToken ct)
    {
        var beneficiaryId = HttpContext.Session.GetInt32(PortalKey); if (beneficiaryId is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(reason)) return BadRequest("Tell us why manual verification is needed.");
        var beneficiary = await db.Beneficiaries.FindAsync([beneficiaryId.Value], ct); if (beneficiary is null || beneficiary.Status == BeneficiaryStatus.Suspended) return NotFound();
        if (beneficiary.Status != BeneficiaryStatus.AwaitingFacialVerification)
            return BadRequest("Complete every required document before requesting manual facial verification.");
        var session = new FacialVerificationSession { Id = Guid.NewGuid(), BeneficiaryId = beneficiary.Id, Status = FacialVerificationStatus.ManualReviewRequired,
            ChallengeJson = "[]", ConsentGranted = false, ConsentNoticeVersion = "manual-alternative-v1", ResultReasonCode = "MANUAL_VERIFICATION_REQUESTED",
            ResultReason = "Manual identity verification was requested.", ExpiresAtUtc = DateTime.UtcNow.AddDays(7), CompletedAtUtc = DateTime.UtcNow };
        db.FacialVerificationSessions.Add(session); beneficiary.ManualReviewReason = reason.Trim();
        db.AuditEntries.Add(new() { EntityType = "Beneficiary", EntityId = beneficiary.Id.ToString(), Action = "Manual facial verification requested" });
        await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Documents));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(CancellationToken ct)
    {
        var beneficiaryId = HttpContext.Session.GetInt32(PortalKey); if (beneficiaryId is null) return Unauthorized();
        var beneficiary = await db.Beneficiaries.Include(x => x.RequirementAssignments).SingleOrDefaultAsync(x => x.Id == beneficiaryId && x.Status != BeneficiaryStatus.Suspended, ct);
        if (beneficiary is null) return NotFound();
        foreach (var assignment in beneficiary.RequirementAssignments.Where(x => x.IsRequired))
        {
            var latest = await db.BeneficiaryDocuments.Where(x => x.BeneficiaryId == beneficiary.Id && x.RequirementId == assignment.RequirementId)
                .OrderByDescending(x => x.UploadedAtUtc).FirstOrDefaultAsync(ct);
            if (latest is null || latest.PreScreenStatus is DocumentPreScreenStatus.ResubmissionRequired or DocumentPreScreenStatus.FailedTechnicalProcessing or DocumentPreScreenStatus.Pending or DocumentPreScreenStatus.Processing)
                return BadRequest("Complete every required document and resolve all resubmission requests before submitting.");
        }
        var face = await db.FacialVerificationSessions.Where(x => x.BeneficiaryId == beneficiary.Id).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (face?.Status is not (FacialVerificationStatus.Verified or FacialVerificationStatus.ManualReviewRequired))
            return BadRequest("Complete live verification or request the manual alternative before submitting.");
        beneficiary.Status = BeneficiaryStatus.UnderAdminReview; beneficiary.SubmittedAtUtc = DateTime.UtcNow;
        if (face.Status == FacialVerificationStatus.ManualReviewRequired && string.IsNullOrWhiteSpace(beneficiary.ManualReviewReason))
            beneficiary.ManualReviewReason = "Heightened Director facial review required.";
        db.AuditEntries.Add(new() { EntityType = "Beneficiary", EntityId = beneficiary.Id.ToString(), Action = "Application submitted",
            SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(new { heightenedReview = face.Status == FacialVerificationStatus.ManualReviewRequired }) });
        await db.SaveChangesAsync(ct); return View("Submitted", beneficiary);
    }
}
