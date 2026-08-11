using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.CurrentUser;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.ViewModels;

namespace SimplexLawFirm.Controllers;

public class CostEstimateController(ApplicationDbContext db, IMatterCostEstimateService estimates, IEstimatePdfService pdf, ICurrentClientService currentClient, INotificationService notifications, IEmailService email) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.MatterTypes = await db.Cases.Where(x => x.CaseType != "").Select(x => x.CaseType).Distinct().OrderBy(x => x).ToListAsync(ct);
        return View(new StartCostEstimateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Begin(StartCostEstimateViewModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.MatterTypes = await db.Cases.Where(x => x.CaseType != "").Select(x => x.CaseType).Distinct().OrderBy(x => x).ToListAsync(ct);
            return View(nameof(Index), input);
        }
        var client = await currentClient.GetAsync(ct);
        try
        {
            var enquiry = await estimates.BeginAsync(input.MatterType, client?.Id, ct);
            var declined = await db.MatterCostEstimates.AnyAsync(x => x.CostEstimateEnquiryId == enquiry.Id && x.Status == CostEstimateStatus.Declined, ct);
            return RedirectToAction(declined ? nameof(Declined) : nameof(Questions), new { token = enquiry.PublicToken });
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Index)); }
    }

    [HttpGet]
    public async Task<IActionResult> Questions(string token, CancellationToken ct)
    {
        var enquiry = await db.CostEstimateEnquiries.Include(x => x.Estimates).SingleOrDefaultAsync(x => x.PublicToken == token, ct);
        if (enquiry == null || enquiry.Estimates.Any()) return NotFound();
        ViewBag.Enquiry = enquiry;
        return View(new CreateCostEstimateViewModel { MatterType = enquiry.MatterType, ContactName = enquiry.ContactName, Email = enquiry.Email });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(string token, CreateCostEstimateViewModel input, CancellationToken ct)
    {
        var enquiry = await db.CostEstimateEnquiries.SingleOrDefaultAsync(x => x.PublicToken == token, ct);
        if (enquiry == null) return NotFound();
        input.MatterType = enquiry.MatterType;
        ModelState.Remove(nameof(input.MatterType));
        if (!ModelState.IsValid) { ViewBag.Enquiry = enquiry; return View("Questions", input); }
        try
        {
            var estimate = await estimates.CalculateAndLockAsync(enquiry.Id, input, ct);
            await email.QueueAsync(enquiry.Email, "Your Simplex matter cost estimate", $"<p>Your locked estimate is ready.</p><p><strong>Estimated range: R {estimate.TotalLow:N2} - R {estimate.TotalHigh:N2}</strong></p><p>This estimate is indicative and not binding.</p>", $"Your estimated range is R {estimate.TotalLow:N2} - R {estimate.TotalHigh:N2}. This estimate is not binding.", $"cost-estimate-{estimate.Id}", ct);
            await db.SaveChangesAsync(ct);
            return RedirectToAction(nameof(Result), new { token });
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); ViewBag.Enquiry = enquiry; return View("Questions", input); }
    }

    [HttpGet]
    public async Task<IActionResult> Result(string token, CancellationToken ct)
    {
        var estimate = await db.MatterCostEstimates.Include(x => x.Enquiry).SingleOrDefaultAsync(x => x.Enquiry.PublicToken == token && x.Status != CostEstimateStatus.Declined, ct);
        if (estimate == null) return NotFound();
        ViewBag.Messages = await db.CostEstimateMessages.Where(x => x.CostEstimateEnquiryId == estimate.CostEstimateEnquiryId).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        return View(estimate);
    }

    [HttpGet]
    public async Task<IActionResult> Declined(string token, CancellationToken ct)
    {
        var estimate = await db.MatterCostEstimates.Include(x => x.Enquiry).SingleOrDefaultAsync(x => x.Enquiry.PublicToken == token && x.Status == CostEstimateStatus.Declined, ct);
        return estimate == null ? NotFound() : View(estimate);
    }

    [HttpGet]
    public async Task<IActionResult> Download(string token, CancellationToken ct)
    {
        var estimate = await db.MatterCostEstimates.Include(x => x.Enquiry).SingleOrDefaultAsync(x => x.Enquiry.PublicToken == token && x.Status != CostEstimateStatus.Declined, ct);
        if (estimate == null) return NotFound();
        return File(pdf.Create(estimate), "application/pdf", $"Simplex-cost-estimate-{estimate.Id:000000}.pdf");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BookConsultation(string token, string contactName, string emailAddress, CancellationToken ct)
    {
        var enquiry = await db.CostEstimateEnquiries.Include(x => x.Estimates).SingleOrDefaultAsync(x => x.PublicToken == token, ct);
        if (enquiry == null) return NotFound();
        var isDeclined = enquiry.Estimates.Any(x => x.Status == CostEstimateStatus.Declined);
        if (isDeclined)
        {
            if (!string.IsNullOrWhiteSpace(contactName)) enquiry.ContactName = contactName.Trim();
            if (!string.IsNullOrWhiteSpace(emailAddress)) enquiry.Email = emailAddress.Trim();
        }
        if (string.IsNullOrWhiteSpace(enquiry.ContactName)
            || string.IsNullOrWhiteSpace(enquiry.Email)
            || !new EmailAddressAttribute().IsValid(enquiry.Email))
        {
            TempData["Error"] = "Provide a valid name and email so we can arrange the consultation.";
            return RedirectToAction(isDeclined ? nameof(Declined) : nameof(Result), new { token });
        }
        enquiry.ConsultationRequestedAtUtc = DateTime.UtcNow;
        foreach (var admin in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct))
            await notifications.QueueAsync(admin, "EstimateConsultation", "Cost estimate consultation requested", $"{enquiry.ContactName} requested a consultation about {enquiry.MatterType}.", $"/CostEstimate/Admin", $"estimate-consultation-{enquiry.Id}-{admin}", ct);
        await email.QueueAsync(enquiry.Email, "Consultation request received", "<p>We received your consultation request and will contact you to arrange a suitable time.</p>", "We received your consultation request and will contact you.", $"estimate-consultation-client-{enquiry.Id}", ct);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Your consultation request has been received.";
        return RedirectToAction(isDeclined ? nameof(Declined) : nameof(Result), new { token });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(string token, string senderName, string body, CancellationToken ct)
    {
        var enquiry = await db.CostEstimateEnquiries.SingleOrDefaultAsync(x => x.PublicToken == token, ct);
        if (enquiry == null) return NotFound();
        if (string.IsNullOrWhiteSpace(body)) { TempData["Error"] = "Enter a message before sending."; return RedirectToAction(nameof(Result), new { token }); }
        db.CostEstimateMessages.Add(new CostEstimateMessage { CostEstimateEnquiryId = enquiry.Id, SenderName = string.IsNullOrWhiteSpace(senderName) ? enquiry.ContactName : senderName.Trim(), Body = body.Trim(), IsFromAdmin = false });
        await db.SaveChangesAsync(ct);
        foreach (var admin in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct))
            await notifications.QueueAsync(admin, "EstimateChat", "New message about a cost estimate", $"{enquiry.ContactName} sent a message about {enquiry.MatterType}.", "/CostEstimate/Admin", $"estimate-chat-{enquiry.Id}-{DateTime.UtcNow.Ticks}-{admin}", ct);
        TempData["Success"] = "Your message has been sent to our team.";
        return RedirectToAction(nameof(Result), new { token });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminReply(int enquiryId, string body, CancellationToken ct)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        var enquiry = await db.CostEstimateEnquiries.SingleOrDefaultAsync(x => x.Id == enquiryId, ct);
        if (enquiry == null) return NotFound();
        var adminId = HttpContext.Session.GetInt32("UserId");
        if (string.IsNullOrWhiteSpace(body)) { TempData["Error"] = "Enter a reply before sending."; return RedirectToAction(nameof(AdminDetails), new { id = await db.MatterCostEstimates.Where(x => x.CostEstimateEnquiryId == enquiryId).Select(x => x.Id).FirstOrDefaultAsync(ct) }); }
        var admin = await db.Users.SingleOrDefaultAsync(x => x.Id == adminId, ct);
        db.CostEstimateMessages.Add(new CostEstimateMessage { CostEstimateEnquiryId = enquiry.Id, SenderName = admin?.FullName ?? "Simplex", Body = body.Trim(), IsFromAdmin = true, AdminUserId = adminId });
        await db.SaveChangesAsync(ct);
        if (!string.IsNullOrWhiteSpace(enquiry.Email))
            await email.QueueAsync(enquiry.Email, "Reply from Simplex", $"<p>{System.Net.WebUtility.HtmlEncode(body.Trim())}</p>", body.Trim(), $"estimate-chat-reply-{enquiry.Id}-{DateTime.UtcNow.Ticks}", ct);
        TempData["Success"] = "Reply sent.";
        var estimateId = await db.MatterCostEstimates.Where(x => x.CostEstimateEnquiryId == enquiryId).Select(x => x.Id).FirstOrDefaultAsync(ct);
        return RedirectToAction(nameof(AdminDetails), new { id = estimateId });
    }

    [HttpGet]
    public async Task<IActionResult> Admin(CancellationToken ct)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        ViewBag.Gaps = await db.CostEstimateCoverageGaps.OrderByDescending(x => x.RecordedAtUtc).ToListAsync(ct);
        return View(await db.MatterCostEstimates.Include(x => x.Enquiry).Include(x => x.LinkedCase).OrderByDescending(x => x.LockedAtUtc).ToListAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> AdminDetails(int id, CancellationToken ct)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        var estimate = await db.MatterCostEstimates.Include(x => x.Enquiry).Include(x => x.LinkedCase).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (estimate == null) return NotFound();
        ViewBag.MatchingCases = await db.Cases.Include(x => x.Client)
            .Where(x => x.Status == CaseStatus.Active
                && x.CaseType == estimate.Enquiry.MatterType
                && x.Client.Email == estimate.Enquiry.Email)
            .ToListAsync(ct);
        ViewBag.Messages = await db.CostEstimateMessages.Where(x => x.CostEstimateEnquiryId == estimate.CostEstimateEnquiryId).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        return View(estimate);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Link(int estimateId, int caseId, CancellationToken ct)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        try { await estimates.LinkToMatterAsync(estimateId, caseId, ct); TempData["Success"] = "Locked estimate linked to the instructed matter."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(AdminDetails), new { id = estimateId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeGap(int id, CancellationToken ct)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        var gap = await db.CostEstimateCoverageGaps.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (gap == null) return NotFound();
        gap.Status = EstimateGapStatus.Acknowledged; gap.AcknowledgedAtUtc = DateTime.UtcNow; gap.AcknowledgedByUserId = HttpContext.Session.GetInt32("UserId");
        await db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Admin));
    }
}
