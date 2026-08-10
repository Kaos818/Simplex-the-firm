using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services.Security;
using SimplexLawFirm.Services;

namespace SimplexLawFirm.Controllers;

public class AppointmentResponseController(ApplicationDbContext db, INotificationService notifications, IEmailService email, IVulnerableClientService safeguards) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Confirm(string token, AppointmentResponseStatus response, CancellationToken ct)
    {
        var invitation = await FindValid(token, ct);
        if (invitation is null || response is not (AppointmentResponseStatus.Accepted or AppointmentResponseStatus.Rejected)) return View("Invalid");
        ViewBag.Token = token; ViewBag.Response = response; return View(invitation.CalendarEvent);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(string token, AppointmentResponseStatus response, string? comments, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var invitation = await FindValid(token, ct);
        if (invitation is null || response is not (AppointmentResponseStatus.Accepted or AppointmentResponseStatus.Rejected)) return BadRequest("This response link is no longer valid.");
        var e = invitation.CalendarEvent;
        if (response == AppointmentResponseStatus.Accepted)
        {
            try { await safeguards.EnsureAppointmentMayConfirmAsync(e.Id, ct); }
            catch (InvalidOperationException ex) { ViewBag.Message = ex.Message; return View("SafeguardRequired", e); }
        }
        e.ClientResponseStatus = response; e.ClientRespondedAtUtc = DateTime.UtcNow; e.ClientResponseComments = comments?[..Math.Min(comments.Length, 500)];
        invitation.UsedAtUtc = DateTime.UtcNow;
        var recipients = new[] { e.CreatedByUserId, e.AssignedToUserId }.Where(x => x.HasValue).Select(x => x!.Value).Distinct();
        foreach (var id in recipients)
        {
            await notifications.QueueAsync(id, "AppointmentResponse", $"Appointment {response}", $"{e.Title} was {response.ToString().ToLowerInvariant()} by the client.", $"/Calendar/EventDetails/{e.Id}", $"appointment-response:{e.Id}:{id}", ct);
            var user = await db.Users.FindAsync([id], ct);
            if (user is not null) await email.QueueAsync(user.Email, $"Appointment {response}", $"<p>{e.Title} was {response.ToString().ToLowerInvariant()} by the client.</p>", $"{e.Title} was {response.ToString().ToLowerInvariant()} by the client.", $"appointment-response-email:{e.Id}:{id}", ct);
        }
        db.AuditEntries.Add(new() { EntityType = "CalendarEvent", EntityId = e.Id.ToString(), Action = response == AppointmentResponseStatus.Accepted ? "Appointment accepted" : "Appointment rejected" });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return View("Completed", e);
    }
    private async Task<AppointmentInvitation?> FindValid(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) return null;
        var hash = SecureToken.Hash(token);
        return await db.AppointmentInvitations.Include(x => x.CalendarEvent).SingleOrDefaultAsync(x => x.TokenHash == hash && x.UsedAtUtc == null && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow, ct);
    }
}
