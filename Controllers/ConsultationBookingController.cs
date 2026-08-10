using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.CurrentUser;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Notifications;

namespace SimplexLawFirm.Controllers;

[RequireSessionRole("Client")]
public sealed class ConsultationBookingController(ApplicationDbContext db, ICurrentClientService currentClient, IEmailService email, INotificationService notifications) : Controller
{
    private const decimal DefaultFee = 1500m;
    private static readonly TimeSpan DayStart = new(9, 0, 0);
    private static readonly TimeSpan DayEnd = new(16, 0, 0);

    [HttpGet]
    public async Task<IActionResult> Index(int? lawyerId, DateTime? date, CancellationToken ct)
    {
        var selectedDate = (date ?? DateTime.Today.AddDays(1)).Date;
        if (selectedDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) selectedDate = selectedDate.AddDays(selectedDate.DayOfWeek == DayOfWeek.Saturday ? 2 : 1);
        ViewBag.Lawyers = await db.Users.Where(x => x.Role == UserRole.Lawyer && x.IsActive).OrderBy(x => x.FullName).ToListAsync(ct);
        ViewBag.Specialties = await db.LawyerProfiles.Include(x => x.Specializations).Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.UserId, x => string.Join(", ", x.Specializations.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).Select(s => s.Name)), ct);
        ViewBag.SelectedLawyerId = lawyerId; ViewBag.SelectedDate = selectedDate;
        if (lawyerId.HasValue)
        {
            var busy = await db.CalendarEvents.Where(x => x.AssignedToUserId == lawyerId && x.Status != EventStatus.Cancelled && x.StartDateTime < selectedDate.AddDays(1) && x.EndDateTime > selectedDate).Select(x => new { x.StartDateTime, x.EndDateTime }).ToListAsync(ct);
            ViewBag.Slots = Enumerable.Range(0, 7).Select(x => selectedDate.Add(DayStart).AddHours(x)).Where(slot => slot > DateTime.Now && !busy.Any(b => b.StartDateTime < slot.AddHours(1) && b.EndDateTime > slot)).ToList();
        }
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestBooking(int lawyerId, DateTime start, string? notes, CancellationToken ct)
    {
        var client = await currentClient.GetAsync(ct); if (client is null) return Forbid();
        if (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || start.TimeOfDay < DayStart || start.TimeOfDay >= DayEnd || start <= DateTime.Now) return BadRequest("Choose an available weekday appointment slot.");
        var end = start.AddHours(1);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var lawyer = await db.Users.SingleOrDefaultAsync(x => x.Id == lawyerId && x.Role == UserRole.Lawyer && x.IsActive, ct); if (lawyer is null) return NotFound();
        if (await db.CalendarEvents.AnyAsync(x => x.AssignedToUserId == lawyerId && x.Status != EventStatus.Cancelled && x.StartDateTime < end && x.EndDateTime > start, ct)) return Conflict("That slot was just taken. Please choose another.");
        var appointment = new CalendarEvent { Title = $"Consultation request: {client.FullName}", Description = notes?.Trim()[..Math.Min(notes.Trim().Length, 2000)] ?? "", Location = "To be confirmed", StartDateTime = start, EndDateTime = end, Type = EventType.Consultation, Status = EventStatus.Scheduled, Color = "#17a2b8", ClientId = client.Id, AssignedToUserId = lawyer.Id, CreatedByUserId = HttpContext.Session.GetInt32("UserId"), AppointmentFee = DefaultFee, PaymentDueDays = 7, ClientResponseStatus = AppointmentResponseStatus.NotRequired, LawyerApprovalStatus = AppointmentApprovalStatus.Pending, Attendees = [], Reminders = [], ChildEvents = [] };
        db.CalendarEvents.Add(appointment); await db.SaveChangesAsync(ct);
        await notifications.QueueAsync(lawyer.Id, "ConsultationRequest", "Consultation approval required", $"{client.FullName} requested {start:dd MMM yyyy HH:mm}.", $"/ConsultationBooking/Review/{appointment.Id}", $"consultation-request:{appointment.Id}:{lawyer.Id}", ct);
        await email.QueueAsync(lawyer.Email, "Consultation approval required", $"<p>{client.FullName} requested a consultation for {start:dd MMM yyyy HH:mm}.</p>", $"{client.FullName} requested a consultation for {start:dd MMM yyyy HH:mm}.", $"consultation-request-email:{appointment.Id}", ct);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); TempData["Success"] = "Your booking request was sent for lawyer approval."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer", "Admin")]
    public async Task<IActionResult> Decide(int id, bool approved, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var appointment = await db.CalendarEvents.Include(x => x.Client).Include(x => x.AssignedToUser).SingleOrDefaultAsync(x => x.Id == id && x.LawyerApprovalStatus == AppointmentApprovalStatus.Pending, ct);
        if (appointment is null) return NotFound();
        if (HttpContext.Session.GetString("UserRole") == "Lawyer" && appointment.AssignedToUserId != userId) return Forbid();
        appointment.LawyerApprovalStatus = approved ? AppointmentApprovalStatus.Approved : AppointmentApprovalStatus.Rejected;
        appointment.ClientResponseStatus = approved ? AppointmentResponseStatus.Accepted : AppointmentResponseStatus.Rejected;
        if (!approved) appointment.Status = EventStatus.Cancelled;
        var outcome = approved ? "approved" : "rejected";
        await email.QueueAsync(appointment.Client!.Email, $"Consultation {outcome}", $"<p>Your consultation request for {appointment.StartDateTime:dd MMM yyyy HH:mm} was {outcome}.</p>", $"Your consultation request was {outcome}.", $"consultation-decision:{appointment.Id}", ct);
        var clientUser = await db.Users.SingleOrDefaultAsync(x => x.Role == UserRole.Client && x.Email.ToUpper() == appointment.Client.Email.ToUpper(), ct);
        if (clientUser is not null) await notifications.QueueAsync(clientUser.Id, "ConsultationDecision", $"Consultation {outcome}", $"Your consultation request was {outcome}.", "/ConsultationBooking", $"consultation-decision:{appointment.Id}:{clientUser.Id}", ct);
        db.AuditEntries.Add(new() { ActorUserId = userId, EntityType = "CalendarEvent", EntityId = appointment.Id.ToString(), Action = $"Consultation {outcome}" });
        await db.SaveChangesAsync(ct); return RedirectToAction("Index", "Calendar");
    }
}
