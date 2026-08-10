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
        if (!IsDirector(role) && role != "Lawyer") return Forbid();
        ViewBag.Role = role;
        ViewBag.Forecasts = await db.CaseForecasts.Include(x => x.Case).Include(x => x.Attorney)
            .Where(x => IsDirector(role) || x.AttorneyId == userId).OrderByDescending(x => x.RequestedAtUtc).Take(8).ToListAsync(ct);
        ViewBag.Handovers = await db.CaseHandovers.Include(x => x.Case).Include(x => x.ReceivingAttorney)
            .Where(x => IsDirector(role) || x.OutgoingAttorneyId == userId || x.ReceivingAttorneyId == userId).OrderByDescending(x => x.CreatedAtUtc).Take(8).ToListAsync(ct);
        ViewBag.Complaints = IsDirector(role) ? await db.ServiceComplaints.Include(x => x.Case).Where(x => x.RoutedToUserId == userId).OrderByDescending(x => x.SubmittedAtUtc).Take(8).ToListAsync(ct) : [];
        ViewBag.Calibration = await db.ForecastCalibrations.Include(x => x.Attorney).OrderBy(x => x.AttorneyId != null).ThenByDescending(x => x.ForecastCount).ToListAsync(ct);
        return View();
    }

    public async Task<IActionResult> Handovers(CancellationToken ct)
    {
        var role = HttpContext.Session.GetString("UserRole");
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || !IsDirector(role) && role != "Lawyer") return Forbid();
        var query = db.CaseHandovers.Include(x => x.Case).Include(x => x.OutgoingAttorney)
            .Include(x => x.ReceivingAttorney).Include(x => x.Items).AsQueryable();
        if (role == "Lawyer")
            query = query.Where(x => x.OutgoingAttorneyId == userId || x.ReceivingAttorneyId == userId);
        ViewBag.Requests = await db.CaseHandoverRequests.Include(x => x.Case).Include(x => x.RequestedByAttorney)
            .Where(x => IsDirector(role) || x.RequestedByAttorneyId == userId).OrderByDescending(x => x.RequestedAtUtc).ToListAsync(ct);
        return View(await query.OrderBy(x => x.Status == HandoverStatus.Accepted)
            .ThenBy(x => x.DueAtUtc).ThenByDescending(x => x.CreatedAtUtc).ToListAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> SelectCaseHandovers(CancellationToken ct)
    {
        var lawyerId = HttpContext.Session.GetInt32("UserId");
        if (HttpContext.Session.GetString("UserRole") != "Lawyer" || !lawyerId.HasValue) return Forbid();
        await LoadSelectableHandoverCasesAsync(lawyerId.Value, ct);
        return View(new SelectCaseHandoversViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectCaseHandovers(SelectCaseHandoversViewModel input, CancellationToken ct)
    {
        var lawyerId = HttpContext.Session.GetInt32("UserId");
        if (HttpContext.Session.GetString("UserRole") != "Lawyer" || !lawyerId.HasValue) return Forbid();
        input.CaseIds = input.CaseIds.Distinct().ToList();
        if (input.CaseIds.Count == 0 || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length < 20)
        {
            ModelState.AddModelError("", "Select at least one open case and provide a clear reason of at least 20 characters.");
            await LoadSelectableHandoverCasesAsync(lawyerId.Value, ct);
            return View(input);
        }

        var matters = await db.Cases.Where(x => input.CaseIds.Contains(x.Id) && x.LawyerId == lawyerId.Value && x.Status == CaseStatus.Active).ToListAsync(ct);
        if (matters.Count != input.CaseIds.Count) return Forbid();
        var previouslyReceived = await db.CaseHandovers.Where(x => input.CaseIds.Contains(x.CaseId) && x.ReceivingAttorneyId == lawyerId && x.Status == HandoverStatus.Accepted).Select(x => x.CaseId).ToListAsync(ct);
        if (previouslyReceived.Count > 0) { TempData["Error"] = "A receiving attorney cannot request another handover for a case already transferred to them. Contact the Director if exceptional intervention is required."; return RedirectToAction(nameof(Handovers)); }
        var pendingCaseIds = await db.CaseHandoverRequests.Where(x => input.CaseIds.Contains(x.CaseId) && x.Status == HandoverRequestStatus.PendingDirector).Select(x => x.CaseId).ToListAsync(ct);
        var eligible = matters.Where(x => !pendingCaseIds.Contains(x.Id)).ToList();
        if (eligible.Count == 0)
        {
            TempData["Error"] = "Every selected case already has a handover request awaiting Director review.";
            return RedirectToAction(nameof(Handovers));
        }

        var reason = input.Reason.Trim();
        var requests = eligible.Select(matter => new CaseHandoverRequest { CaseId = matter.Id, RequestedByAttorneyId = lawyerId.Value, Reason = reason }).ToList();
        db.CaseHandoverRequests.AddRange(requests);
        await db.SaveChangesAsync(ct);
        var directorIds = await db.Users.Where(x => (x.Role == UserRole.Admin || x.Role == UserRole.Director) && x.IsActive).Select(x => x.Id).ToListAsync(ct);
        foreach (var request in requests)
        {
            var matter = eligible.Single(x => x.Id == request.CaseId);
            foreach (var directorId in directorIds)
                await notifications.QueueAsync(directorId, "HandoverRequest", $"Handover requested for {matter.CaseNumber}", reason, $"/Practice/ReviewHandoverRequest/{request.Id}", $"handover-request-{request.Id}-{directorId}", ct);
        }
        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"{requests.Count} case handover request{(requests.Count == 1 ? " was" : "s were")} sent to the Director." + (pendingCaseIds.Count > 0 ? $" {pendingCaseIds.Count} existing pending request(s) were skipped." : "");
        return RedirectToAction(nameof(Handovers));
    }

    private async Task LoadSelectableHandoverCasesAsync(int lawyerId, CancellationToken ct)
    {
        var pending = db.CaseHandoverRequests.Where(x => x.Status == HandoverRequestStatus.PendingDirector).Select(x => x.CaseId);
        var received = db.CaseHandovers.Where(x => x.ReceivingAttorneyId == lawyerId && x.Status == HandoverStatus.Accepted).Select(x => x.CaseId);
        ViewBag.Cases = await db.Cases.Include(x => x.Client)
            .Where(x => x.LawyerId == lawyerId && x.Status == CaseStatus.Active && !pending.Contains(x.Id) && !received.Contains(x.Id))
            .OrderBy(x => x.CaseNumber).ToListAsync(ct);
    }

    [HttpGet]
    public async Task<IActionResult> RequestHandover(int caseId, CancellationToken ct)
    {
        var lawyerId = HttpContext.Session.GetInt32("UserId");
        if (HttpContext.Session.GetString("UserRole") != "Lawyer" || !lawyerId.HasValue) return Forbid();
        var matter = await db.Cases.Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == caseId && x.LawyerId == lawyerId && x.Status == CaseStatus.Active, ct);
        if (matter is null) return NotFound();
        if (await db.CaseHandovers.AnyAsync(x => x.CaseId == caseId && x.ReceivingAttorneyId == lawyerId && x.Status == HandoverStatus.Accepted, ct)) { TempData["Error"] = "Cases already transferred to you cannot be handed over again. Contact the Director for exceptional intervention."; return RedirectToAction(nameof(Handovers)); }
        ViewBag.Case = matter; return View(new CaseHandoverRequest { CaseId = caseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestHandover(CaseHandoverRequest input, CancellationToken ct)
    {
        var lawyerId = HttpContext.Session.GetInt32("UserId");
        if (HttpContext.Session.GetString("UserRole") != "Lawyer" || !lawyerId.HasValue) return Forbid();
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == input.CaseId && x.LawyerId == lawyerId && x.Status == CaseStatus.Active, ct);
        if (matter is null) return Forbid();
        if (await db.CaseHandovers.AnyAsync(x => x.CaseId == input.CaseId && x.ReceivingAttorneyId == lawyerId && x.Status == HandoverStatus.Accepted, ct)) { TempData["Error"] = "You cannot request another handover for a case already transferred to you. Contact the Director for exceptional intervention."; return RedirectToAction(nameof(Handovers)); }
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length < 20) { TempData["Error"] = "Provide a clear handover reason of at least 20 characters."; return RedirectToAction(nameof(RequestHandover), new { caseId = input.CaseId }); }
        if (await db.CaseHandoverRequests.AnyAsync(x => x.CaseId == input.CaseId && x.Status == HandoverRequestStatus.PendingDirector, ct)) { TempData["Error"] = "A handover request is already awaiting review."; return RedirectToAction(nameof(Handovers)); }
        input.RequestedByAttorneyId = lawyerId.Value; input.Reason = input.Reason.Trim(); db.CaseHandoverRequests.Add(input); await db.SaveChangesAsync(ct);
        foreach(var directorId in await db.Users.Where(x => (x.Role == UserRole.Admin || x.Role == UserRole.Director) && x.IsActive).Select(x => x.Id).ToListAsync(ct)) await notifications.QueueAsync(directorId, "HandoverRequest", $"Handover requested for {matter.CaseNumber}", input.Reason, $"/Practice/ReviewHandoverRequest/{input.Id}", $"handover-request-{input.Id}-{directorId}", ct);
        await db.SaveChangesAsync(ct); TempData["Success"] = "The Director was notified and responsibility remains with you until transfer is accepted."; return RedirectToAction(nameof(Handovers));
    }

    [HttpGet]
    public async Task<IActionResult> ReviewHandoverRequest(int id, CancellationToken ct)
    {
        if (!IsDirector(HttpContext.Session.GetString("UserRole"))) return Forbid();
        var request = await db.CaseHandoverRequests.Include(x => x.Case).ThenInclude(x => x.Client).Include(x => x.RequestedByAttorney).SingleOrDefaultAsync(x => x.Id == id, ct); if (request is null) return NotFound();
        ViewBag.Lawyers = await db.Users.Where(x => x.Role == UserRole.Lawyer && x.IsActive && x.Id != request.RequestedByAttorneyId).OrderBy(x => x.FullName).ToListAsync(ct); return View(request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewHandoverRequest(int id, bool approve, int? receivingAttorneyId, string response, CancellationToken ct)
    {
        if (!IsDirector(HttpContext.Session.GetString("UserRole"))) return Forbid();
        var request = await db.CaseHandoverRequests.Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == id && x.Status == HandoverRequestStatus.PendingDirector, ct); if (request is null) return NotFound();
        if (string.IsNullOrWhiteSpace(response) || approve && !receivingAttorneyId.HasValue) { TempData["Error"] = "Record a response and select the receiving lawyer when approving."; return RedirectToAction(nameof(ReviewHandoverRequest), new { id }); }
        request.DirectorResponse = response.Trim(); request.ReviewedAtUtc = DateTime.UtcNow; request.ReviewedByDirectorId = HttpContext.Session.GetInt32("UserId");
        if (approve)
        {
            try { var handover = await service.ApproveReassignmentAsync(request.CaseId, receivingAttorneyId!.Value, request.ReviewedByDirectorId.Value, request.DirectorResponse, ct); request.Status = HandoverRequestStatus.Approved; request.CaseHandoverId = handover.Id; }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(ReviewHandoverRequest), new { id }); }
        }
        else request.Status = HandoverRequestStatus.Declined;
        await notifications.QueueAsync(request.RequestedByAttorneyId, "HandoverDecision", approve ? "Handover approved" : "Handover declined", request.DirectorResponse, approve ? $"/Practice/Handover/{request.CaseHandoverId}" : "/Practice/Handovers", $"handover-decision-{request.Id}", ct);
        await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Handovers));
    }

    public async Task<IActionResult> ClientHandovers(CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        return View(await db.CaseHandoverRequests.Include(x => x.Case).Include(x => x.RequestedByAttorney).Where(x => x.Case.ClientId == client.Id).OrderByDescending(x => x.RequestedAtUtc).ToListAsync(ct));
    }

    public async Task<IActionResult> Forecast(int id, CancellationToken ct)
    {
        var matter = await db.Cases.Include(x => x.Lawyer).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (matter == null) return NotFound();
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("UserRole");
        if (userId is null || (!IsDirector(role) && matter.LawyerId != userId)) return Forbid();
        var forecast = await db.CaseForecasts.Where(x => x.CaseId == id).OrderByDescending(x => x.RequestedAtUtc).FirstOrDefaultAsync(ct);
        ViewBag.SupportingDocuments = await db.Documents.Where(x => x.CaseId == id).OrderByDescending(x => x.UploadedAt).ToListAsync(ct);
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
        if (!IsDirector(HttpContext.Session.GetString("UserRole"))) return Forbid();
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
        if (!IsDirector(HttpContext.Session.GetString("UserRole"))) return Forbid();
        var matter = await db.Cases.Include(x => x.Lawyer).SingleOrDefaultAsync(x => x.Id == id && x.Status != CaseStatus.Closed && x.Status != CaseStatus.Archived, ct);
        if (matter == null) return NotFound();
        ViewBag.Case = matter;
        ViewBag.Lawyers = await db.Users.Where(x => x.Role == UserRole.Lawyer && x.IsActive && x.Id != matter.LawyerId).OrderBy(x => x.FullName).ToListAsync(ct);
        return View(new ReassignmentViewModel { CaseId = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reassign(ReassignmentViewModel input, CancellationToken ct)
    {
        if (!IsDirector(HttpContext.Session.GetString("UserRole"))) return Forbid();
        if (!ModelState.IsValid) return RedirectToAction(nameof(Reassign), new { id = input.CaseId });
        try { var handover = await service.ApproveReassignmentAsync(input.CaseId, input.ReceivingAttorneyId, HttpContext.Session.GetInt32("UserId")!.Value, input.Reason, ct); return RedirectToAction(nameof(Handover), new { id = handover.Id }); }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Reassign), new { id = input.CaseId }); }
    }

    public async Task<IActionResult> Handover(int id, CancellationToken ct)
    {
        var handover = await db.CaseHandovers.Include(x => x.Case).Include(x => x.OutgoingAttorney).Include(x => x.ReceivingAttorney).Include(x => x.CaseReassignment).Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (handover == null) return NotFound();
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null || (!IsDirector(HttpContext.Session.GetString("UserRole")) && handover.OutgoingAttorneyId != userId && handover.ReceivingAttorneyId != userId)) return Forbid();
        if (handover.Status is HandoverStatus.Preparing or HandoverStatus.Overdue or HandoverStatus.QueryRaised)
            await service.RefreshHandoverAsync(handover, ct);
        return View(new HandoverPageViewModel { Handover = handover });
    }

    public async Task<IActionResult> Correspondence(int id, CancellationToken ct)
    {
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (matter == null) return NotFound();
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!IsDirector(HttpContext.Session.GetString("UserRole")) && matter.LawyerId != userId) return Forbid();
        ViewBag.Case = matter;
        ViewBag.UnsignedDocuments = await db.Documents.Where(x => x.CaseId == id && x.RequiresSignature && x.SignedAtUtc == null).OrderByDescending(x => x.UploadedAt).ToListAsync(ct);
        return View(await db.ClientCorrespondence.Where(x => x.CaseId == id).OrderByDescending(x => x.ReceivedAtUtc).ToListAsync(ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordCorrespondence(int caseId, string subject, CancellationToken ct)
    {
        var matter = await db.Cases.SingleOrDefaultAsync(x => x.Id == caseId, ct);
        var userId = HttpContext.Session.GetInt32("UserId");
        if (matter == null || (!IsDirector(HttpContext.Session.GetString("UserRole")) && matter.LawyerId != userId)) return Forbid();
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
        if (item == null || (!IsDirector(HttpContext.Session.GetString("UserRole")) && item.Case.LawyerId != userId)) return Forbid();
        item.AnsweredAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Correspondence), new { id = item.CaseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDocumentSigned(int id, int caseId, CancellationToken ct)
    {
        var document = await db.Documents.Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == id && x.CaseId == caseId, ct);
        var userId = HttpContext.Session.GetInt32("UserId");
        if (document == null || (!IsDirector(HttpContext.Session.GetString("UserRole")) && document.Case.LawyerId != userId)) return Forbid();
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
        await db.SaveChangesAsync(ct);
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
    public async Task<IActionResult> AcceptHandover(AcceptHandoverViewModel input, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var handover = await db.CaseHandovers.Include(x => x.Case).Include(x => x.CaseReassignment).Include(x => x.ReceivingAttorney).Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == input.Id && x.ReceivingAttorneyId == userId, ct);
        if (handover == null) return Forbid();
        if (handover.Status != HandoverStatus.Ready) { TempData["Error"] = "This handover is not ready for acceptance."; return RedirectToAction(nameof(Handover), new { id = input.Id }); }
        var mandatoryIds = handover.Items.Where(x => x.IsMandatory).Select(x => x.Id).OrderBy(x => x).ToArray();
        var acknowledgedIds = input.AcknowledgedItemIds.Distinct().OrderBy(x => x).ToArray();
        if (!mandatoryIds.SequenceEqual(acknowledgedIds)) { TempData["Error"] = "Acknowledge every mandatory checklist item before accepting responsibility."; return RedirectToAction(nameof(Handover), new { id = input.Id }); }
        if (!input.RiskAcknowledged) { TempData["Error"] = "Acknowledge the Director's risk flags before accepting responsibility."; return RedirectToAction(nameof(Handover), new { id = input.Id }); }
        if (!string.Equals(input.SignatureName?.Trim(), handover.ReceivingAttorney.FullName, StringComparison.OrdinalIgnoreCase)) { TempData["Error"] = $"Type your full name exactly as {handover.ReceivingAttorney.FullName} to sign."; return RedirectToAction(nameof(Handover), new { id = input.Id }); }
        if (handover.Case.LawyerId != handover.OutgoingAttorneyId)
        {
            TempData["Error"] = "Responsibility no longer matches the approved handover. A Director must review the matter before acceptance.";
            return RedirectToAction(nameof(Handover), new { id = input.Id });
        }
        handover.Status = HandoverStatus.Accepted; handover.AcceptedAtUtc = DateTime.UtcNow; handover.Case.LawyerId = handover.ReceivingAttorneyId;
        handover.AcceptanceSignatureName = input.SignatureName.Trim(); handover.AcceptanceRiskAcknowledged = true; handover.ReceivingAttorneyRespondedAtUtc = DateTime.UtcNow;
        handover.CaseReassignment.Status = ReassignmentStatus.Completed;
        db.AuditEntries.Add(new() { ActorUserId = userId, EntityType = "CaseHandover", EntityId = handover.Id.ToString(), Action = "Receiving attorney accepted responsibility" });
        await notifications.QueueAsync(handover.OutgoingAttorneyId, "HandoverAccepted", "Case handover accepted", $"{handover.ReceivingAttorney.FullName} accepted responsibility for {handover.Case.CaseNumber}.", $"/Practice/Handover/{handover.Id}", $"handover-accepted-{handover.Id}-{handover.OutgoingAttorneyId}", ct);
        foreach (var directorId in await db.Users.Where(x => (x.Role == UserRole.Admin || x.Role == UserRole.Director) && x.IsActive).Select(x => x.Id).ToListAsync(ct)) await notifications.QueueAsync(directorId, "HandoverAccepted", "Case handover completed", $"Responsibility for {handover.Case.CaseNumber} transferred to {handover.ReceivingAttorney.FullName}.", $"/Practice/Handover/{handover.Id}", $"handover-accepted-{handover.Id}-{directorId}", ct);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Handover accepted. Responsibility has transferred to you.";
        return RedirectToAction(nameof(Handover), new { id = input.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QueryHandover(int id, string query, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var handover = await db.CaseHandovers.Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == id && x.ReceivingAttorneyId == userId, ct);
        if (handover == null) return Forbid();
        if (handover.Status != HandoverStatus.Ready || handover.DirectorQueryVerdictFinal) { TempData["Error"] = "The Director's response is the final verdict for this handover. You must accept or decline responsibility."; return RedirectToAction(nameof(Handover), new { id }); }
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 20) { TempData["Error"] = "Explain your question about the Director's decision in at least 20 characters."; return RedirectToAction(nameof(RaiseHandoverQuery), new { id }); }
        handover.Status = HandoverStatus.QueryRaised; handover.ReceivingAttorneyQuery = query.Trim()[..Math.Min(query.Trim().Length, 2000)]; handover.ReceivingAttorneyRespondedAtUtc = DateTime.UtcNow;
        handover.DirectorQueryResponse = null; handover.DirectorQueryRespondedByUserId = null; handover.DirectorQueryRespondedAtUtc = null;
        await notifications.QueueAsync(handover.OutgoingAttorneyId, "HandoverQuery", "Receiving attorney raised a handover query", handover.ReceivingAttorneyQuery, $"/Practice/Handover/{id}", $"handover-query-{id}-{handover.OutgoingAttorneyId}", ct);
        foreach (var directorId in await db.Users.Where(x => (x.Role == UserRole.Admin || x.Role == UserRole.Director) && x.IsActive).Select(x => x.Id).ToListAsync(ct)) await notifications.QueueAsync(directorId, "HandoverQuery", "Handover query requires your response", $"{handover.Case.CaseNumber}: {handover.ReceivingAttorneyQuery}", $"/Practice/ReviewHandoverQuery/{id}", $"handover-query-{id}-{directorId}", ct);
        db.AuditEntries.Add(new() { ActorUserId = userId, EntityType = "CaseHandover", EntityId = id.ToString(), Action = "Receiving attorney raised query" }); await db.SaveChangesAsync(ct);
        TempData["Success"] = "Your query was sent to the Director for a response."; return RedirectToAction(nameof(Handover), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> RaiseHandoverQuery(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var handover = await db.CaseHandovers.Include(x => x.Case).ThenInclude(x => x.Client).Include(x => x.OutgoingAttorney).Include(x => x.ReceivingAttorney).Include(x => x.CaseReassignment).SingleOrDefaultAsync(x => x.Id == id && x.ReceivingAttorneyId == userId, ct);
        if (handover == null) return Forbid();
        if (handover.DirectorQueryVerdictFinal || handover.Status is not (HandoverStatus.Ready or HandoverStatus.QueryRaised)) return RedirectToAction(nameof(Handover), new { id });
        return View(handover);
    }

    [HttpGet]
    public async Task<IActionResult> ReviewHandoverQuery(int id, CancellationToken ct)
    {
        if (!IsDirector(HttpContext.Session.GetString("UserRole"))) return Forbid();
        var handover = await db.CaseHandovers.Include(x => x.Case).ThenInclude(x => x.Client).Include(x => x.OutgoingAttorney).Include(x => x.ReceivingAttorney).Include(x => x.CaseReassignment).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (handover == null) return NotFound();
        if (string.IsNullOrWhiteSpace(handover.ReceivingAttorneyQuery)) return RedirectToAction(nameof(Handover), new { id });
        return View(handover);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RespondToHandoverQuery(int id, string response, CancellationToken ct)
    {
        if (!IsDirector(HttpContext.Session.GetString("UserRole"))) return Forbid();
        var directorId = HttpContext.Session.GetInt32("UserId");
        var handover = await db.CaseHandovers.Include(x => x.Case).Include(x => x.ReceivingAttorney).SingleOrDefaultAsync(x => x.Id == id && x.Status == HandoverStatus.QueryRaised, ct);
        if (handover == null) return NotFound();
        if (string.IsNullOrWhiteSpace(response) || response.Trim().Length < 20) { TempData["Error"] = "Provide a relevant response of at least 20 characters."; return RedirectToAction(nameof(ReviewHandoverQuery), new { id }); }
        handover.DirectorQueryResponse = response.Trim()[..Math.Min(response.Trim().Length, 2000)];
        handover.DirectorQueryRespondedByUserId = directorId; handover.DirectorQueryRespondedAtUtc = DateTime.UtcNow; handover.DirectorQueryVerdictFinal = true; handover.Status = HandoverStatus.Ready;
        db.AuditEntries.Add(new() { ActorUserId = directorId, EntityType = "CaseHandover", EntityId = id.ToString(), Action = "Director responded to receiving attorney query" });
        await notifications.QueueAsync(handover.ReceivingAttorneyId, "HandoverQueryResponse", "Director responded to your handover query", $"{handover.Case.CaseNumber}: {handover.DirectorQueryResponse}", $"/Practice/Handover/{id}", $"handover-query-response-{id}-{handover.ReceivingAttorneyId}-{handover.DirectorQueryRespondedAtUtc:yyyyMMddHHmmss}", ct);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"Response sent to {handover.ReceivingAttorney.FullName}. The handover is ready for their decision.";
        return RedirectToAction(nameof(Handover), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineHandover(int id, string reason, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var handover = await db.CaseHandovers.Include(x => x.Case).Include(x => x.CaseReassignment).SingleOrDefaultAsync(x => x.Id == id && x.ReceivingAttorneyId == userId, ct);
        if (handover == null) return Forbid();
        if (handover.Status != HandoverStatus.Ready) { TempData["Error"] = "Only a ready handover can be declined."; return RedirectToAction(nameof(Handover), new { id }); }
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 20) { TempData["Error"] = "Provide a clear reason of at least 20 characters."; return RedirectToAction(nameof(Handover), new { id }); }
        handover.Status = HandoverStatus.Declined; handover.ReceivingAttorneyDeclineReason = reason.Trim()[..Math.Min(reason.Trim().Length, 2000)]; handover.ReceivingAttorneyRespondedAtUtc = DateTime.UtcNow; handover.CaseReassignment.Status = ReassignmentStatus.Cancelled;
        await notifications.QueueAsync(handover.OutgoingAttorneyId, "HandoverDeclined", "Receiving attorney declined handover", handover.ReceivingAttorneyDeclineReason, $"/Practice/Handover/{id}", $"handover-declined-{id}-{handover.OutgoingAttorneyId}", ct);
        foreach (var directorId in await db.Users.Where(x => (x.Role == UserRole.Admin || x.Role == UserRole.Director) && x.IsActive).Select(x => x.Id).ToListAsync(ct)) await notifications.QueueAsync(directorId, "HandoverDeclined", "Handover reassignment requires review", $"{handover.Case.CaseNumber}: {handover.ReceivingAttorneyDeclineReason}", $"/Practice/Handover/{id}", $"handover-declined-{id}-{directorId}", ct);
        db.AuditEntries.Add(new() { ActorUserId = userId, EntityType = "CaseHandover", EntityId = id.ToString(), Action = "Receiving attorney declined handover" }); await db.SaveChangesAsync(ct);
        TempData["Success"] = "The handover was declined and returned to the Director for reassignment."; return RedirectToAction(nameof(Handover), new { id });
    }

    public async Task<IActionResult> LodgeComplaint(CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct);
        if (client == null) return Forbid();
        ViewBag.Cases = await db.Cases.Where(x => x.ClientId == client.Id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return View(new LodgeComplaintViewModel());
    }

    public async Task<IActionResult> MyComplaints(CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client == null) return Forbid();
        return View(await db.ServiceComplaints.Include(x => x.Case).Where(x => x.ClientId == client.Id).OrderByDescending(x => x.SubmittedAtUtc).ToListAsync(ct));
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
        var complaint = client == null ? null : await db.ServiceComplaints.Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == id && x.ClientId == client.Id, ct);
        return complaint == null ? NotFound() : View(complaint);
    }

    [HttpGet("Practice/Complaints/{id:int}")]
    public async Task<IActionResult> Complaint(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var complaint = await db.ServiceComplaints.Include(x => x.Case).Include(x => x.Client).Include(x => x.RoutedToUser).Include(x => x.Attachments).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (complaint == null) return NotFound();
        var restricted = complaint.RestrictedUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
        var client = await currentClient.GetAsync(ct);
        if (userId is null || client?.Id != complaint.ClientId && (complaint.RoutedToUserId != userId || restricted.Contains(userId.Value))) return Forbid();
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
    public async Task<IActionResult> RespondToComplaint(int id, string feedback, bool resolve, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var complaint = await db.ServiceComplaints.Include(x => x.Client).Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == id && x.RoutedToUserId == userId, ct);
        if (complaint == null) return Forbid();
        if (string.IsNullOrWhiteSpace(feedback)) { TempData["Error"] = "Record meaningful feedback for the client."; return RedirectToAction(nameof(Complaint), new { id }); }
        complaint.DirectorFeedback = feedback.Trim(); complaint.RespondedAtUtc = DateTime.UtcNow; complaint.Status = resolve ? ComplaintStatus.Resolved : ComplaintStatus.Acknowledged;
        var clientUser = await db.Users.SingleOrDefaultAsync(x => x.Email == complaint.Client.Email, ct);
        if (clientUser is not null) await notifications.QueueAsync(clientUser.Id, "ComplaintFeedback", $"Feedback on {complaint.ReferenceNumber}", complaint.DirectorFeedback, $"/Practice/Complaints/{complaint.Id}", $"complaint-feedback-{complaint.Id}-{complaint.RespondedAtUtc:O}", ct);
        await db.SaveChangesAsync(ct); TempData["Success"] = resolve ? "Feedback sent and complaint resolved." : "Feedback sent to the client.";
        return RedirectToAction(nameof(Complaint), new { id });
    }
    private static bool IsDirector(string? role) => role is "Admin" or "Director";
}
