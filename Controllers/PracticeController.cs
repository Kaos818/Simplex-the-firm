using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.CurrentUser;
using SimplexLawFirm.ViewModels;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Storage;
using SimplexLawFirm.Services.Notifications;

namespace SimplexLawFirm.Controllers;

[RequireSessionUser]
public class PracticeController(ApplicationDbContext db, IPracticeIntelligenceService service, ICurrentClientService currentClient, IComplaintFileStorage complaintFiles, IEmailService email, INotificationService notifications, IPrecedentLibraryService precedents) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var role = HttpContext.Session.GetString("UserRole");
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null) return RedirectToAction("Login", "Home");
        if (role is not ("Admin" or "Lawyer")) return Forbid();
        ViewBag.Role = role;
        ViewBag.Forecasts = await db.CaseForecasts.Include(x => x.Case).Include(x => x.Attorney)
            .Where(x => role == "Admin" || x.AttorneyId == userId).OrderByDescending(x => x.RequestedAtUtc).Take(8).ToListAsync(ct);
        ViewBag.Handovers = await db.CaseHandovers.Include(x => x.Case).Include(x => x.ReceivingAttorney)
            .Where(x => role == "Admin" || x.OutgoingAttorneyId == userId || x.ReceivingAttorneyId == userId).OrderByDescending(x => x.CreatedAtUtc).Take(8).ToListAsync(ct);
        ViewBag.Complaints = role == "Admin" ? await db.ServiceComplaints.Include(x => x.Case).Where(x => x.RoutedToUserId == userId).OrderByDescending(x => x.SubmittedAtUtc).Take(8).ToListAsync(ct) : [];
        ViewBag.Calibration = await db.ForecastCalibrations.Include(x => x.Attorney).OrderBy(x => x.AttorneyId != null).ThenByDescending(x => x.ForecastCount).ToListAsync(ct);
        if (role == "Admin")
        {
            var busyCaseIds = await db.CaseHandovers.Where(x => x.Status != HandoverStatus.Accepted).Select(x => x.CaseId).ToListAsync(ct);
            ViewBag.ReassignableCases = await db.Cases.Include(x => x.Lawyer).Where(x => x.LawyerId != null
                && x.Status != CaseStatus.Closed && x.Status != CaseStatus.Archived && !busyCaseIds.Contains(x.Id))
                .OrderBy(x => x.CaseNumber).ToListAsync(ct);
        }
        return View();
    }

    public async Task<IActionResult> Forecast(int id, CancellationToken ct)
    {
        var matter = await db.Cases.Include(x => x.Lawyer).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (matter == null) return NotFound();
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("UserRole");
        if (userId is null || (role != "Admin" && matter.LawyerId != userId)) return Forbid();
        var forecast = await db.CaseForecasts.Where(x => x.CaseId == id).OrderByDescending(x => x.RequestedAtUtc).FirstOrDefaultAsync(ct);
        return View(new ForecastPageViewModel { Case = matter, Forecast = forecast });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestForecast(int caseId, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || HttpContext.Session.GetString("UserRole") != "Lawyer") return Forbid();
        try { await service.CreateForecastAsync(caseId, userId.Value, ct); TempData["Success"] = "Forecast created. Record your professional assessment to lock the snapshot."; }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Forecast), new { id = caseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordAssessment(int forecastId, int caseId, decimal attorneyAssessmentPercent, bool attorneyAgrees, string? attorneyNotes, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || !await db.CaseForecasts.AnyAsync(x => x.Id == forecastId && x.AttorneyId == userId, ct)) return Forbid();
        try { await service.LockForecastAsync(forecastId, attorneyAssessmentPercent, attorneyAgrees, attorneyNotes, ct); TempData["Success"] = "Your professional assessment has been committed."; }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Forecast), new { id = caseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseAndScore(int caseId, ForecastResult outcome, string? outcomeSummary, bool outcomeIsPrivileged, bool outcomeIsConfidential, CancellationToken ct)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        try { await service.ScoreForecastAsync(caseId, outcome, ct); }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Forecast), new { id = caseId }); }
        var matter = await db.Cases.SingleAsync(x => x.Id == caseId, ct);
        matter.OutcomeSummary = outcomeSummary?.Trim();
        matter.OutcomeIsPrivileged = outcomeIsPrivileged;
        matter.OutcomeIsConfidential = outcomeIsConfidential;
        await db.SaveChangesAsync(ct);
        await precedents.QueueCaseOutcomeAsync(caseId, ct);
        TempData["Success"] = "Matter closed and forecast accuracy recorded.";
        return RedirectToAction(nameof(Forecast), new { id = caseId });
    }

    public async Task<IActionResult> Reassign(int id, CancellationToken ct)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        var matter = await db.Cases.Include(x => x.Lawyer).SingleOrDefaultAsync(x => x.Id == id && x.Status != CaseStatus.Closed && x.Status != CaseStatus.Archived, ct);
        if (matter == null) return NotFound();
        ViewBag.Case = matter;
        ViewBag.Lawyers = await db.Users.Where(x => x.Role == UserRole.Lawyer && x.IsActive && x.Id != matter.LawyerId).OrderBy(x => x.FullName).ToListAsync(ct);
        return View(new ReassignmentViewModel { CaseId = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reassign(ReassignmentViewModel input, CancellationToken ct)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        if (!ModelState.IsValid) return RedirectToAction(nameof(Reassign), new { id = input.CaseId });
        try { var handover = await service.ApproveReassignmentAsync(input.CaseId, input.ReceivingAttorneyId, HttpContext.Session.GetInt32("UserId")!.Value, input.Reason, ct); return RedirectToAction(nameof(Handover), new { id = handover.Id }); }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Reassign), new { id = input.CaseId }); }
    }

    public async Task<IActionResult> Handover(int id, CancellationToken ct)
    {
        var handover = await db.CaseHandovers.Include(x => x.Case).ThenInclude(x => x.Documents).Include(x => x.OutgoingAttorney).Include(x => x.ReceivingAttorney).Include(x => x.DirectorReviewedByUser)
            .Include(x => x.Items).Include(x => x.Queries).ThenInclude(x => x.RaisedByUser).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (handover == null) return NotFound();
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || (HttpContext.Session.GetString("UserRole") != "Admin" && handover.OutgoingAttorneyId != userId && handover.ReceivingAttorneyId != userId)) return Forbid();
        return View(new HandoverPageViewModel { Handover = handover });
    }

    public async Task<IActionResult> Correspondence(int id, CancellationToken ct)
    {
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (matter == null) return NotFound();
        var userId = HttpContext.Session.GetInt32("UserId");
        if (HttpContext.Session.GetString("UserRole") != "Admin" && matter.LawyerId != userId) return Forbid();
        ViewBag.Case = matter;
        ViewBag.UnsignedDocuments = await db.Documents.Where(x => x.CaseId == id && x.RequiresSignature && x.SignedAtUtc == null).OrderByDescending(x => x.UploadedAt).ToListAsync(ct);
        return View(await db.ClientCorrespondence.Where(x => x.CaseId == id).OrderByDescending(x => x.ReceivedAtUtc).ToListAsync(ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordCorrespondence(int caseId, string subject, CancellationToken ct)
    {
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == caseId, ct);
        var userId = HttpContext.Session.GetInt32("UserId");
        if (matter == null || (HttpContext.Session.GetString("UserRole") != "Admin" && matter.LawyerId != userId)) return Forbid();
        if (string.IsNullOrWhiteSpace(subject)) { TempData["Error"] = "Enter a correspondence subject."; return RedirectToAction(nameof(Correspondence), new { id = caseId }); }
        db.ClientCorrespondence.Add(new ClientCorrespondence { CaseId = caseId, Subject = subject.Trim() });
        await db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Correspondence), new { id = caseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCorrespondenceAnswered(int id, CancellationToken ct)
    {
        var item = await db.ClientCorrespondence.Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == id, ct);
        var userId = HttpContext.Session.GetInt32("UserId");
        if (item == null || (HttpContext.Session.GetString("UserRole") != "Admin" && item.Case.LawyerId != userId)) return Forbid();
        item.AnsweredAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Correspondence), new { id = item.CaseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDocumentSigned(int id, int caseId, CancellationToken ct)
    {
        var document = await db.Documents.Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == id && x.CaseId == caseId, ct);
        var userId = HttpContext.Session.GetInt32("UserId");
        if (document == null || (HttpContext.Session.GetString("UserRole") != "Admin" && document.Case.LawyerId != userId)) return Forbid();
        document.SignedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Correspondence), new { id = caseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveItem(int id, int handoverId, bool resolved, string? resolutionNote, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || !await db.CaseHandovers.AnyAsync(x => x.Id == handoverId && x.OutgoingAttorneyId == userId, ct)) return Forbid();
        var item = await db.HandoverItems.SingleOrDefaultAsync(x => x.Id == id && x.CaseHandoverId == handoverId, ct);
        if (item == null) return NotFound();
        var handover = await db.CaseHandovers.SingleAsync(x => x.Id == handoverId, ct);
        if (handover.Status is not (HandoverStatus.Preparing or HandoverStatus.Overdue))
        {
            TempData["Error"] = "This handover is no longer editable.";
            return RedirectToAction(nameof(Handover), new { id = handoverId });
        }
        if (resolved && item.IsMandatory && string.IsNullOrWhiteSpace(resolutionNote))
        {
            TempData["Error"] = "Record the position taken before clearing a mandatory item.";
            return RedirectToAction(nameof(Handover), new { id = handoverId });
        }
        item.IsResolved = resolved; item.ResolutionNote = resolutionNote;
        if (resolved) item.DirectorDisputeNote = null;
        await db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Handover), new { id = handoverId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DisputeHandoverItem(int handoverId, int itemId, string note, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null) return Forbid();
        try { await service.DisputeHandoverItemAsync(handoverId, itemId, userId.Value, note, ct); TempData["Success"] = "Item reopened and the outgoing attorney has been notified."; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Handover), new { id = handoverId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReady(int id, string? notes, string? urgentMatters, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || !await db.CaseHandovers.AnyAsync(x => x.Id == id && x.OutgoingAttorneyId == userId, ct)) return Forbid();
        var handover = await db.CaseHandovers.SingleAsync(x => x.Id == id, ct);
        handover.Notes = notes; handover.UrgentMatters = urgentMatters; await db.SaveChangesAsync(ct);
        try
        {
            if (await service.MarkHandoverReadyAsync(id, ct)) TempData["Success"] = "Handover ready. The receiving attorney and Director have been notified.";
            else TempData["Error"] = "The handover is still blocked. Clear every mandatory live item first.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Handover), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptHandover(int id, string signature, bool riskFlagsAcknowledged, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null) return Forbid();
        try { await service.AcceptHandoverAsync(id, userId.Value, signature, riskFlagsAcknowledged, ct); TempData["Success"] = "Handover accepted. Responsibility has transferred to you."; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Handover), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitDirectorReview(int handoverId, bool approve, bool checksConfirmed, string? summary, string? riskFlags, string? returnReason, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        if (approve && !checksConfirmed) { TempData["Error"] = "All director verification checks must be confirmed before approval."; return RedirectToAction(nameof(Handover), new { id = handoverId }); }
        try
        {
            await service.SubmitDirectorReviewAsync(handoverId, userId.Value, approve, summary, riskFlags, returnReason, ct);
            TempData["Success"] = approve ? "Handover approved and released to the receiving attorney." : "Handover returned to the outgoing attorney.";
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Handover), new { id = handoverId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeItem(int handoverId, int itemId, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null) return Forbid();
        try { await service.AcknowledgeHandoverItemAsync(handoverId, itemId, userId.Value, ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Handover), new { id = handoverId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RaiseHandoverQuery(int handoverId, string question, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null) return Forbid();
        try { await service.RaiseHandoverQueryAsync(handoverId, userId.Value, question, ct); TempData["Success"] = "Query sent."; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Handover), new { id = handoverId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineHandover(int handoverId, string reason, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null) return Forbid();
        try { await service.DeclineHandoverAcceptanceAsync(handoverId, userId.Value, reason, ct); TempData["Success"] = "Acceptance declined. The handover has been returned to the director."; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Handover), new { id = handoverId });
    }

    public async Task<IActionResult> LodgeComplaint(CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct);
        if (client == null) return Forbid();
        ViewBag.Cases = await db.Cases.Where(x => x.ClientId == client.Id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return View(new LodgeComplaintViewModel());
    }

    public async Task<IActionResult> Prospects(CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct);
        if (client == null) return Forbid();
        ViewBag.Cases = await db.Cases.Where(x => x.ClientId == client.Id && x.Status != CaseStatus.Archived).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        ViewBag.Requests = await db.ClientForecastRequests.Include(x => x.Case).Include(x => x.FulfilledByForecast).Where(x => x.ClientId == client.Id).OrderByDescending(x => x.RequestedAtUtc).ToListAsync(ct);
        return View(new RequestProspectsViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestProspects(RequestProspectsViewModel input, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct);
        if (client == null) return Forbid();
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == input.CaseId && x.ClientId == client.Id && x.Status != CaseStatus.Closed && x.Status != CaseStatus.Archived, ct);
        if (matter == null) { TempData["Error"] = "Choose one of your open matters."; return RedirectToAction(nameof(Prospects)); }
        if (!matter.LawyerId.HasValue) { TempData["Error"] = "This matter must have an assigned attorney before prospects can be assessed."; return RedirectToAction(nameof(Prospects)); }
        if (await db.ClientForecastRequests.AnyAsync(x => x.CaseId == matter.Id && x.Status == ForecastRequestStatus.Pending, ct))
        { TempData["Error"] = "A prospects assessment is already pending for this matter."; return RedirectToAction(nameof(Prospects)); }
        var request = new ClientForecastRequest { CaseId = matter.Id, ClientId = client.Id, ClientMessage = input.Message?.Trim() ?? "" };
        db.ClientForecastRequests.Add(request);
        await db.SaveChangesAsync(ct);
        await notifications.QueueAsync(matter.LawyerId.Value, "ForecastRequest", "Client requested prospects assessment", $"The client requested an outcome forecast for {matter.CaseNumber}.", $"/Practice/Forecast/{matter.Id}", $"forecast-request-{request.Id}", ct);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Your attorney has been asked to prepare an assessment of prospects.";
        return RedirectToAction(nameof(Prospects));
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit((5 * ComplaintFileStorage.MaximumBytes) + (1024 * 1024))]
    public async Task<IActionResult> LodgeComplaint(LodgeComplaintViewModel input, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct);
        if (client == null) return Forbid();
        if (!ModelState.IsValid) { ViewBag.Cases = await db.Cases.Where(x => x.ClientId == client.Id).ToListAsync(ct); return View(input); }
        try
        {
            if (input.Attachments.Count > 5) throw new InvalidDataException("A maximum of five attachments is allowed.");
            var duplicate = await db.ServiceComplaints.AnyAsync(x => x.ClientId == client.Id && x.CaseId == input.CaseId && x.Category == input.Category && x.Status != ComplaintStatus.Resolved, ct);
            if (duplicate && !input.ConfirmPossibleDuplicate)
            {
                ModelState.AddModelError(nameof(input.ConfirmPossibleDuplicate), "A similar complaint is already open. Review the warning and confirm only if this is a separate complaint.");
                ViewBag.RequiresDuplicateConfirmation = true;
                ViewBag.Cases = await db.Cases.Where(x => x.ClientId == client.Id).ToListAsync(ct);
                return View(input);
            }
            var complaint = await service.LodgeComplaintAsync(client.Id, input, ct);
            foreach (var file in input.Attachments.Where(x => x.Length > 0))
            {
                var stored = await complaintFiles.StoreAsync(complaint.Id, file, ct);
                db.ComplaintAttachments.Add(new ComplaintAttachment { ServiceComplaintId = complaint.Id, OriginalFileName = stored.OriginalFileName, RelativePath = stored.RelativePath, ContentType = stored.ContentType, SizeBytes = stored.SizeBytes, Sha256Hash = stored.Sha256Hash });
            }
            await email.QueueAsync(client.Email, $"Complaint received: {complaint.ReferenceNumber}", $"<p>Your complaint has been securely received.</p><p><strong>{complaint.ReferenceNumber}</strong></p><p>Response due: {complaint.ResponseDueAtUtc:dd MMMM yyyy}</p>", $"Your complaint {complaint.ReferenceNumber} was received. Response due {complaint.ResponseDueAtUtc:dd MMMM yyyy}.", $"complaint-receipt-{complaint.Id}", ct);
            await db.SaveChangesAsync(ct);
            return RedirectToAction(nameof(ComplaintReceipt), new { id = complaint.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException)
        {
            ModelState.AddModelError("", ex.Message); ViewBag.RequiresDuplicateConfirmation = ex.Message.Contains("similar complaint", StringComparison.OrdinalIgnoreCase); ViewBag.Cases = await db.Cases.Where(x => x.ClientId == client.Id).ToListAsync(ct); return View(input);
        }
    }

    public async Task<IActionResult> ComplaintReceipt(int id, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct);
        var complaint = client == null ? null : await db.ServiceComplaints.Include(x => x.Case).Include(x => x.Appointments).SingleOrDefaultAsync(x => x.Id == id && x.ClientId == client.Id, ct);
        return complaint == null ? NotFound() : View(complaint);
    }

    [HttpGet("Practice/Complaints/{id:int}")]
    public async Task<IActionResult> Complaint(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var complaint = await db.ServiceComplaints.Include(x => x.Case).Include(x => x.Client).Include(x => x.RoutedToUser).Include(x => x.Attachments)
            .Include(x => x.ResolvedByUser).Include(x => x.Appointments).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (complaint == null) return NotFound();
        var restricted = complaint.RestrictedUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
        if (userId is null || complaint.RoutedToUserId != userId || restricted.Contains(userId.Value)) return Forbid();
        ViewBag.ServiceRecords = await db.StaffServiceRecordEntries.Include(x => x.StaffUser).Where(x => x.ServiceComplaintId == id).ToListAsync(ct);
        return View(complaint);
    }

    public async Task<IActionResult> DownloadComplaintAttachment(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var attachment = await db.ComplaintAttachments.Include(x => x.ServiceComplaint).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (attachment == null) return NotFound();
        var complaint = attachment.ServiceComplaint;
        var restricted = complaint.RestrictedUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
        var client = await currentClient.GetAsync(ct);
        if ((complaint.RoutedToUserId != userId || restricted.Contains(userId)) && client?.Id != complaint.ClientId) return Forbid();
        return File(await complaintFiles.OpenReadAsync(attachment.RelativePath, ct), attachment.ContentType, attachment.OriginalFileName);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeComplaint(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var complaint = await db.ServiceComplaints.SingleOrDefaultAsync(x => x.Id == id && x.RoutedToUserId == userId, ct);
        if (complaint == null) return Forbid();
        complaint.Status = ComplaintStatus.Acknowledged; complaint.AcknowledgedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Complaint acknowledged and response clock stopped.";
        return RedirectToAction(nameof(Complaint), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveComplaint(int complaintId, ComplaintResolutionOutcome outcome, List<string> mediationSteps, string? customStep, string formalResponse, string? remedy, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null) return Forbid();
        if (!string.IsNullOrWhiteSpace(customStep)) mediationSteps.Add(customStep);
        try
        {
            var complaint = await service.ResolveComplaintAsync(complaintId, userId.Value, outcome, mediationSteps, formalResponse, remedy, ct);
            await email.QueueAsync(complaint.Client.Email, $"Complaint resolved: {complaint.ReferenceNumber}",
                $"<h2>Your complaint has been resolved</h2><p><strong>{complaint.ReferenceNumber}</strong></p><p>{System.Net.WebUtility.HtmlEncode(complaint.FormalResponse)}</p>",
                $"Your complaint {complaint.ReferenceNumber} has been resolved. {complaint.FormalResponse}", $"complaint-resolved-{complaint.Id}", ct);
            TempData["Success"] = "Resolution recorded and the client has been notified.";
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Complaint), new { id = complaintId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BookComplaintAppointment(int complaintId, DateTime scheduledAtUtc, AppointmentFormat format, string? notes, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null) return Forbid();
        try { await service.BookComplaintAppointmentAsync(complaintId, userId.Value, scheduledAtUtc, format, notes, ct); TempData["Success"] = "Appointment booked."; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        var role = HttpContext.Session.GetString("UserRole");
        return role == "Client" ? RedirectToAction(nameof(ComplaintReceipt), new { id = complaintId }) : RedirectToAction(nameof(Complaint), new { id = complaintId });
    }
}
