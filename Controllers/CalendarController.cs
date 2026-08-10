using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using TaskItemStatus = SimplexLawFirm.Models.TaskStatus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Security;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services;
using SimplexLawFirm.ViewModels.Calendar;

namespace SimplexLawFirm.Controllers
{
    [RequireSessionRole("Director", "Admin", "Lawyer", "Paralegal")]
    public class CalendarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notifications;
        private readonly IVulnerableClientService _safeguards;

        public CalendarController(ApplicationDbContext context, IEmailService email, IConfiguration configuration, INotificationService notifications, IVulnerableClientService safeguards)
        {
            _context = context;
            _email = email;
            _configuration = configuration;
            _notifications = notifications;
            _safeguards = safeguards;
        }

        
        // MAIN CALENDAR VIEW
        public async Task<IActionResult> Index(string view = "month", int? caseId = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            var isLawyer = userRole?.Equals("Lawyer", StringComparison.OrdinalIgnoreCase) == true;
            var activeUsers = await _context.Users.Where(u => u.IsActive && u.Role == UserRole.Lawyer).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.ActiveUsers = activeUsers;
            ViewBag.LawyerLegend = activeUsers.Select(u => new KeyValuePair<string, string>(u.FullName, ColorForLawyer(u.Id))).ToList();
            var accessibleCases = _context.Cases.Where(c => c.Status != CaseStatus.Closed);
            if (isLawyer) accessibleCases = accessibleCases.Where(c => c.LawyerId == userId);
            var clientIds = accessibleCases.Select(c => c.ClientId).Distinct();
            ViewBag.Clients = await _context.Clients.Where(c => c.IsActive && clientIds.Contains(c.Id)).OrderBy(c => c.FirstName).ThenBy(c => c.LastName).ToListAsync();
            ViewBag.Retainers = await _context.Retainers.Where(r => r.Status == RetainerStatus.Active && clientIds.Contains(r.ClientId)).OrderBy(r => r.Title).ToListAsync();

            // Get cases for dropdown
            var cases = await accessibleCases.OrderBy(c => c.Title).ToListAsync();
            var clientNames = await _context.Clients.Where(c => clientIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.IsBusiness ? c.CompanyName! : (c.FirstName + " " + c.LastName));
            ViewBag.Cases = new SelectList(cases.Select(c => new { c.Id, Label = $"{c.Title} — {clientNames.GetValueOrDefault(c.ClientId, "Client")}" }), "Id", "Label");
            ViewBag.CaseClientMapJson = JsonConvert.SerializeObject(cases.ToDictionary(c => c.Id.ToString(), c => c.ClientId));

            ViewBag.CurrentView = view;
            ViewBag.UserRole = userRole;
            ViewBag.SelectedCaseId = caseId;

            // Get events for the current user
            var events = await _context.CalendarEvents
                .Include(e => e.Case)
                .Include(e => e.AssignedToUser)
                .Include(e => e.Client)
                .Where(e => e.Status != EventStatus.Cancelled)
                .ToListAsync();

            // Filter based on role
            if (!HasFirmWideCalendarAccess(userRole))
            {
                events = events.Where(e => e.AssignedToUserId == userId || e.CreatedByUserId == userId).ToList();
            }

            var calendarItems = events.Select(e => new CalendarFeedItem(
                e.Id.ToString(), e.Title,
                DateTime.SpecifyKind(e.StartDateTime, DateTimeKind.Utc).ToString("o"),
                DateTime.SpecifyKind(e.EndDateTime, DateTimeKind.Utc).ToString("o"),
                e.IsAllDay, HasFirmWideCalendarAccess(userRole) ? ColorForLawyer(e.AssignedToUserId) : (string.IsNullOrWhiteSpace(e.Color) ? ColorFor(e.Type) : e.Color),
                e.Type.ToString(), e.Status.ToString(), e.Client?.FullName,
                e.AssignedToUser == null ? "Unassigned" : e.AssignedToUser.FullName, e.Location)).ToList();

            var taskQuery = _context.Tasks.Include(x => x.AssignedTo).Include(x => x.Case).ThenInclude(x => x!.Client).AsQueryable();
            if (!HasFirmWideCalendarAccess(userRole))
                taskQuery = taskQuery.Where(x => x.AssignedToId == userId || x.CreatedById == userId);
            var tasks = await taskQuery.Where(x => x.Status != TaskItemStatus.Completed && x.DueDate.HasValue).ToListAsync();
            calendarItems.AddRange(tasks.Select(t => new CalendarFeedItem(
                $"task-{t.Id}", $"Task: {t.Title}",
                DateTime.SpecifyKind(t.DueDate!.Value, DateTimeKind.Utc).ToString("o"),
                DateTime.SpecifyKind(t.DueDate.Value.AddMinutes(30), DateTimeKind.Utc).ToString("o"),
                false, t.Priority is TaskPriority.Urgent ? "#b42318" : t.Priority is TaskPriority.High ? "#b54708" : "#6941c6",
                "Task", t.Status.ToString(), t.Case?.Client?.FullName,
                t.AssignedTo?.FullName ?? "Unassigned", "Task deadline")));

            ViewBag.EventsJson = JsonConvert.SerializeObject(calendarItems);

            return View();
        }

        [HttpGet]
        public IActionResult Create(int? caseId = null) => RedirectToAction(nameof(Index), new { caseId });

        // GET CALENDAR EVENTS (AJAX)
        [HttpGet]
        public IActionResult ClientIndex() => RedirectToAction(nameof(Index));

        public IActionResult LawyerIndex() => RedirectToAction(nameof(Index));

        public async Task<IActionResult> GetEvents(DateTime start, DateTime end)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            var query = _context.CalendarEvents
                .Include(e => e.Case)
                .Include(e => e.AssignedToUser)
                .Include(e => e.Client)
                .Where(e => e.Status != EventStatus.Cancelled && e.StartDateTime < end && e.EndDateTime > start);

            if (!HasFirmWideCalendarAccess(userRole))
            {
                query = query.Where(e => e.AssignedToUserId == userId || e.CreatedByUserId == userId);
            }

            var events = await query.ToListAsync();

            var eventList = events.Select(e => new
            {
                id = e.Id,
                title = e.Title,
                start = e.StartDateTime.ToString("o"),
                end = e.EndDateTime.ToString("o"),
                allDay = e.IsAllDay,
                color = HasFirmWideCalendarAccess(userRole) ? ColorForLawyer(e.AssignedToUserId) : (string.IsNullOrWhiteSpace(e.Color) ? ColorFor(e.Type) : e.Color),
                description = e.Description,
                location = e.Location,
                type = e.Type.ToString(),
                status = e.Status.ToString(),
                caseId = e.CaseId,
                caseTitle = e.Case != null ? e.Case.Title : null,
                assignedTo = e.AssignedToUser != null ? e.AssignedToUser.FullName : null,
                clientName = e.Client != null ? e.Client.FullName : null
            });

            return Json(eventList);
        }

        public static bool HasFirmWideCalendarAccess(string? role) =>
            role is not null && (role.Equals("Director", StringComparison.OrdinalIgnoreCase) || role.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        private static string ColorFor(EventType type) => type switch
        {
            EventType.CourtAppearance or EventType.Hearing => "#b42318",
            EventType.Deadline or EventType.FilingDeadline => "#b54708",
            EventType.Meeting or EventType.Mediation => "#067647",
            EventType.Appointment or EventType.Consultation => "#175cd3",
            _ => "#344054"
        };

        internal static string ColorForLawyer(int? lawyerId)
        {
            string[] palette = ["#175cd3", "#067647", "#b54708", "#7a5af8", "#c11574", "#026aa2", "#912018", "#107569", "#6938ef", "#a15c07"];
            return lawyerId.HasValue ? palette[Math.Abs((long)lawyerId.Value) % palette.Length] : "#667085";
        }

        private sealed record CalendarFeedItem(string id, string title, string start, string end, bool allDay,
            string color, string type, string status, string? clientName, string? assignedTo, string? location);

        // CREATE EVENT
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent([FromBody] CreateCalendarEventRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Json(new { success = false, error = "Event data is null" });
                }

                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Json(new { success = false, error = "Event title is required" });
                }

                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, error = "User not logged in" });
                }

                var userRole = HttpContext.Session.GetString("UserRole");
                var isLawyer = userRole?.Equals("Lawyer", StringComparison.OrdinalIgnoreCase) == true;
                if (isLawyer) request.AssignedToUserId = userId;
                if (!isLawyer && !request.AssignedToUserId.HasValue)
                    return Json(new { success = false, error = "Please select the lawyer responsible for this event." });
                if (!request.CaseId.HasValue)
                    return Json(new { success = false, error = "Please select the related case." });
                var selectedCase = await _context.Cases.SingleOrDefaultAsync(x => x.Id == request.CaseId);
                if (selectedCase is null) return Json(new { success = false, error = "The selected case was not found." });
                if (isLawyer && selectedCase.LawyerId != userId) return Forbid();
                if (!isLawyer && selectedCase.LawyerId != request.AssignedToUserId)
                    return Json(new { success = false, error = "The selected case is not assigned to that lawyer." });
                if (!request.ClientId.HasValue) return Json(new { success = false, error = "Please select the client." });
                if (request.ClientId != selectedCase.ClientId) return Json(new { success = false, error = "The selected case does not belong to that client." });
                if (request.EndLocal <= request.StartLocal) return Json(new { success = false, error = "End time must be after start time." });
                if (string.IsNullOrWhiteSpace(request.Location) && string.IsNullOrWhiteSpace(request.MeetingLink))
                    return Json(new { success = false, error = "Enter a place or online meeting link." });
                var billableAppointment = request.Type is EventType.Appointment or EventType.Consultation;
                var clientFacing = true;
                if (billableAppointment && (request.AppointmentFee is null or <= 0 || request.PaymentDueDays <= 0))
                    return Json(new { success = false, error = "Client, positive fee, valid times, payment terms, and a location or meeting link are required." });
                var client = await _context.Clients.FindAsync(request.ClientId);
                if (client is null) return Json(new { success = false, error = "Client was not found." });
                if (request.RetainerId is not null && !await _context.Retainers.AnyAsync(r => r.Id == request.RetainerId && r.ClientId == request.ClientId && r.Status == RetainerStatus.Active))
                    return Json(new { success = false, error = "The selected retainer is not active for this client." });
                var startUtc = request.StartLocal.ToUniversalTime();
                var endUtc = request.EndLocal.ToUniversalTime();
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var adjustedEnd = clientFacing
                    ? await _safeguards.ApplyDurationAsync(request.ClientId!.Value, startUtc, endUtc)
                    : endUtc;
                var eventData = new CalendarEvent {
                    Title = request.Title.Trim(), Description = request.Description ?? "", Location = request.Location ?? "", MeetingLink = request.MeetingLink ?? "",
                    StartDateTime = startUtc, EndDateTime = adjustedEnd, Type = request.Type, IsAllDay = request.IsAllDay,
                    AssignedToUserId = request.AssignedToUserId, CaseId = request.CaseId, RetainerId = request.RetainerId, ClientId = request.ClientId,
                    AppointmentFee = billableAppointment ? request.AppointmentFee : null, PaymentDueDays = request.PaymentDueDays,
                    LatePenaltyType = request.LatePenaltyType, LatePenaltyValue = request.LatePenaltyValue, LatePenaltyGraceDays = request.LatePenaltyGraceDays,
                    ClientResponseStatus = clientFacing ? AppointmentResponseStatus.Pending : AppointmentResponseStatus.NotRequired,
                    LawyerApprovalStatus = clientFacing ? AppointmentApprovalStatus.Approved : AppointmentApprovalStatus.NotRequired,
                    CreatedByUserId = userId, CreatedAt = DateTime.UtcNow, Status = EventStatus.Scheduled, CompletionNotes = "", RecurrenceRule = "", Color = ""
                };

                if (string.IsNullOrEmpty(eventData.Color))
                {
                    eventData.Color = eventData.Type switch
                    {
                        EventType.CourtAppearance => "#dc3545",
                        EventType.Hearing => "#dc3545",
                        EventType.Deadline => "#fd7e14",
                        EventType.FilingDeadline => "#fd7e14",
                        EventType.Meeting => "#28a745",
                        EventType.Mediation => "#28a745",
                        EventType.Appointment => "#17a2b8",
                        EventType.Consultation => "#17a2b8",
                        EventType.Deposition => "#6c757d",
                        _ => "#6c757d"
                    };
                }

                _context.CalendarEvents.Add(eventData);
                await _context.SaveChangesAsync();
                if (clientFacing)
                {
                    var token = SecureToken.Create();
                    var expires = eventData.StartDateTime > DateTime.UtcNow ? eventData.StartDateTime : DateTime.UtcNow.AddHours(1);
                    _context.AppointmentInvitations.Add(new AppointmentInvitation { CalendarEventId = eventData.Id, TokenHash = token.Hash, CreatedAtUtc = DateTime.UtcNow, ExpiresAtUtc = expires });
                    var root = ResolvePublicBaseUrl();
                    var accept = $"{root}/AppointmentResponse/Confirm?token={Uri.EscapeDataString(token.Raw)}&response=Accepted";
                    var reject = $"{root}/AppointmentResponse/Confirm?token={Uri.EscapeDataString(token.Raw)}&response=Rejected";
                    var penalty = request.LatePenaltyType switch { LatePenaltyType.FixedAmount => $"R{request.LatePenaltyValue:N2} after {request.LatePenaltyGraceDays} grace days", LatePenaltyType.Percentage => $"{request.LatePenaltyValue:N2}% after {request.LatePenaltyGraceDays} grace days", _ => "No late penalty" };
                    var zone = GetJohannesburgTimeZone();
                    var startLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(eventData.StartDateTime, DateTimeKind.Utc), zone);
                    var endLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(eventData.EndDateTime, DateTimeKind.Utc), zone);
                    var creator = await _context.Users.FindAsync(userId.Value);
                    var staff = eventData.AssignedToUserId is not null ? await _context.Users.FindAsync(eventData.AssignedToUserId.Value) : creator;
                    var venue = string.IsNullOrWhiteSpace(eventData.Location) ? "Online appointment" : eventData.Location;
                    var meeting = string.IsNullOrWhiteSpace(eventData.MeetingLink) ? "No online meeting link" : eventData.MeetingLink;
                    var description = string.IsNullOrWhiteSpace(eventData.Description) ? "No additional description" : eventData.Description;
                    var billing = billableAppointment ? $"\nFee: R{eventData.AppointmentFee:N2}\nPayment due within {eventData.PaymentDueDays} days. Late-payment terms: {penalty}.\nBilling occurs only after you accept and the appointment has ended." : "";
                    var text = $"{eventData.Title}\nLawyer: {staff?.FullName ?? creator?.FullName ?? "Simplex Law Firm"}\nCase: {selectedCase.Title}\nDate: {startLocal:dddd, dd MMMM yyyy}\nTime: {startLocal:HH:mm} to {endLocal:HH:mm} (Africa/Johannesburg)\nLocation: {venue}\nMeeting link: {meeting}\nDescription: {description}{billing}\nAccept: {accept}\nDecline: {reject}";
                    var encoded = System.Net.WebUtility.HtmlEncode(text).Replace("\n", "<br>");
                    var html = $"<p>{encoded}</p><p><a style=\"display:inline-block;padding:10px 16px;background:#198754;color:white;text-decoration:none\" href=\"{System.Net.WebUtility.HtmlEncode(accept)}\">Accept event</a> &nbsp; <a style=\"display:inline-block;padding:10px 16px;background:#dc3545;color:white;text-decoration:none\" href=\"{System.Net.WebUtility.HtmlEncode(reject)}\">Decline event</a></p>";
                    await _email.QueueAsync(client.Email, $"Event invitation: {eventData.Title}", html, text, $"appointment-invite:{eventData.Id}", HttpContext.RequestAborted);
                    if (!isLawyer && staff is not null && staff.Id != userId)
                    {
                        var staffText = $"Hello {staff.FullName},\n\nA new appointment, {eventData.Title}, has been scheduled for you with {client.FullName} on {startLocal:dddd, dd MMMM yyyy 'at' HH:mm}.";
                        await _email.QueueAsync(staff.Email, $"New appointment assigned: {eventData.Title}", $"<p>Hello {System.Net.WebUtility.HtmlEncode(staff.FullName)},</p><p>A new appointment <strong>{System.Net.WebUtility.HtmlEncode(eventData.Title)}</strong> has been scheduled for you with {System.Net.WebUtility.HtmlEncode(client.FullName)} on {startLocal:dddd, dd MMMM yyyy 'at' HH:mm}.</p><p><a href=\"/Calendar/EventDetails/{eventData.Id}\">View appointment</a></p>", staffText, $"appointment-lawyer:{eventData.Id}:{staff.Id}", HttpContext.RequestAborted);
                        await _notifications.QueueAsync(staff.Id, "CalendarEvent", "New appointment assigned", $"{eventData.Title} with {client.FullName} is scheduled for {startLocal:dd MMM yyyy HH:mm}.", $"/Calendar/EventDetails/{eventData.Id}", $"appointment-lawyer:{eventData.Id}:{staff.Id}", HttpContext.RequestAborted);
                    }
                    var clientUser = await _context.Users.SingleOrDefaultAsync(x => x.Role == UserRole.Client && x.Email.ToUpper() == client.Email.ToUpper());
                    if (clientUser is not null)
                        await _notifications.QueueAsync(clientUser.Id, "AppointmentInvitation", "New appointment invitation",
                            $"{eventData.Title} is scheduled for {startLocal:dd MMM yyyy HH:mm}. Check your email to accept or reject it.", "/ClientCalendar", $"appointment-invite-notification:{eventData.Id}", HttpContext.RequestAborted);
                    _context.AuditEntries.Add(new() { ActorUserId = userId, EntityType = "CalendarEvent", EntityId = eventData.Id.ToString(), Action = "Appointment created" });
                    _context.AuditEntries.Add(new() { ActorUserId = userId, EntityType = "CalendarEvent", EntityId = eventData.Id.ToString(), Action = "Invitation sent" });
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var lawyer = await _context.Users.FindAsync(eventData.AssignedToUserId!.Value);
                    var caseClient = await _context.Clients.FindAsync(eventData.ClientId!.Value);
                    var zone = GetJohannesburgTimeZone();
                    var localStart = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(eventData.StartDateTime, DateTimeKind.Utc), zone);
                    var details = $"{eventData.Title} is scheduled for {localStart:dddd, dd MMMM yyyy 'at' HH:mm} (Africa/Johannesburg). Case: {selectedCase.Title}. Location: {(string.IsNullOrWhiteSpace(eventData.Location) ? "To be confirmed" : eventData.Location)}.";
                    if (lawyer is not null && lawyer.Id != userId)
                    {
                        await _email.QueueAsync(lawyer.Email, $"New case event: {eventData.Title}", $"<p>Hello {System.Net.WebUtility.HtmlEncode(lawyer.FullName)},</p><p>{System.Net.WebUtility.HtmlEncode(details)}</p><p><a href=\"/Calendar/EventDetails/{eventData.Id}\">View event details</a></p>", details, $"case-event-lawyer:{eventData.Id}:{lawyer.Id}", HttpContext.RequestAborted);
                        await _notifications.QueueAsync(lawyer.Id, "CalendarEvent", "New case event", details, $"/Calendar/EventDetails/{eventData.Id}", $"case-event:{eventData.Id}:{lawyer.Id}", HttpContext.RequestAborted);
                    }
                    if (caseClient is not null)
                        await _email.QueueAsync(caseClient.Email, $"Your case event: {eventData.Title}", $"<p>Hello {System.Net.WebUtility.HtmlEncode(caseClient.FullName)},</p><p>{System.Net.WebUtility.HtmlEncode(details)}</p><p>Your lawyer is {System.Net.WebUtility.HtmlEncode(lawyer?.FullName ?? "your assigned attorney")}.</p>", details, $"case-event-client:{eventData.Id}:{caseClient.Id}", HttpContext.RequestAborted);
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();
                return Json(new { success = true, eventId = eventData.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        private static TimeZoneInfo GetJohannesburgTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time"); }
        }

        private string ResolvePublicBaseUrl()
        {
            var configured = _configuration["Email:PublicBaseUrl"]?.Trim().TrimEnd('/');
            if (Uri.TryCreate(configured, UriKind.Absolute, out var configuredUri) && configuredUri.Scheme is "https" or "http")
                return configuredUri.GetLeftPart(UriPartial.Authority) + configuredUri.AbsolutePath.TrimEnd('/');

            if (Request.Host.HasValue)
                return $"{Request.Scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');

            throw new InvalidOperationException("Email:PublicBaseUrl must be configured before sending calendar invitations.");
        }

        // UPDATE EVENT
        [HttpPut, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] CreateCalendarEventRequest eventData)
        {
            try
            {
                var existingEvent = await _context.CalendarEvents.FindAsync(id);
                if (existingEvent == null)
                {
                    return Json(new { success = false, error = "Event not found" });
                }

                var userId = HttpContext.Session.GetInt32("UserId");
                var role = HttpContext.Session.GetString("UserRole");
                if (!HasFirmWideCalendarAccess(role) && existingEvent.AssignedToUserId != userId && existingEvent.CreatedByUserId != userId) return Forbid();
                if (role?.Equals("Lawyer", StringComparison.OrdinalIgnoreCase) == true)
                {
                    eventData.AssignedToUserId = userId;
                    if (!eventData.CaseId.HasValue || !await _context.Cases.AnyAsync(x => x.Id == eventData.CaseId && x.LawyerId == userId)) return Forbid();
                }
                if (!eventData.CaseId.HasValue || !eventData.AssignedToUserId.HasValue)
                    return Json(new { success = false, error = "A lawyer and related case are required." });
                var updatedMatter = await _context.Cases.SingleOrDefaultAsync(x => x.Id == eventData.CaseId && x.Status != CaseStatus.Closed);
                if (updatedMatter is null || updatedMatter.LawyerId != eventData.AssignedToUserId)
                    return Json(new { success = false, error = "The selected case is not assigned to that lawyer." });
                if (eventData.ClientId != updatedMatter.ClientId)
                    return Json(new { success = false, error = "The selected case does not belong to that client." });
                var startUtc = eventData.StartLocal.ToUniversalTime();
                var endUtc = eventData.EndLocal.ToUniversalTime();
                if (endUtc <= startUtc) return Json(new { success = false, error = "End time must be after start time." });

                existingEvent.Title = eventData.Title;
                existingEvent.Description = eventData.Description ?? "";
                existingEvent.Location = eventData.Location ?? "";
                existingEvent.StartDateTime = startUtc;
                existingEvent.EndDateTime = existingEvent.ClientId.HasValue
                    ? await _safeguards.ApplyDurationAsync(existingEvent.ClientId.Value, startUtc, endUtc)
                    : endUtc;
                existingEvent.IsAllDay = eventData.IsAllDay;
                existingEvent.Type = eventData.Type;
                existingEvent.MeetingLink = eventData.MeetingLink ?? "";
                existingEvent.AssignedToUserId = eventData.AssignedToUserId;
                existingEvent.CaseId = eventData.CaseId;
                existingEvent.ClientId = eventData.ClientId;
                existingEvent.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // DRAG & DROP UPDATE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveEvent(int id, DateTime newStart, DateTime newEnd)
        {
            try
            {
                var existingEvent = await _context.CalendarEvents.FindAsync(id);
                if (existingEvent == null)
                {
                    return Json(new { success = false });
                }

                var userId = HttpContext.Session.GetInt32("UserId");
                if (!HasFirmWideCalendarAccess(HttpContext.Session.GetString("UserRole")) && existingEvent.AssignedToUserId != userId && existingEvent.CreatedByUserId != userId) return Forbid();

                var timeDifference = newEnd - newStart;
                existingEvent.StartDateTime = newStart;
                existingEvent.EndDateTime = existingEvent.ClientId.HasValue
                    ? await _safeguards.ApplyDurationAsync(existingEvent.ClientId.Value, newStart, newEnd) : newEnd;
                existingEvent.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }

        // GET: Calendar/GetEvent/{id} - Get single event for editing
        [HttpGet]
        public async Task<IActionResult> GetEvent(int id)
        {
            var calendarEvent = await _context.CalendarEvents
                .FirstOrDefaultAsync(e => e.Id == id);

            if (calendarEvent == null)
            {
                return Json(new { success = false, error = "Event not found" });
            }
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!HasFirmWideCalendarAccess(HttpContext.Session.GetString("UserRole")) && calendarEvent.AssignedToUserId != userId && calendarEvent.CreatedByUserId != userId) return Forbid();

            return Json(new
            {
                id = calendarEvent.Id,
                title = calendarEvent.Title,
                type = (int)calendarEvent.Type,
                start = DateTime.SpecifyKind(calendarEvent.StartDateTime, DateTimeKind.Utc),
                end = DateTime.SpecifyKind(calendarEvent.EndDateTime, DateTimeKind.Utc),
                location = calendarEvent.Location,
                description = calendarEvent.Description,
                assignedToUserId = calendarEvent.AssignedToUserId,
                caseId = calendarEvent.CaseId,
                clientId = calendarEvent.ClientId,
                allDay = calendarEvent.IsAllDay
            });
        }

        // GET: Calendar/GetUpcomingEvents - Already there but ensure it's correct
        // Your existing method is fine

        // RESIZE EVENT
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResizeEvent(int id, DateTime newEnd)
        {
            try
            {
                var existingEvent = await _context.CalendarEvents.FindAsync(id);
                if (existingEvent == null)
                {
                    return Json(new { success = false });
                }

                var userId = HttpContext.Session.GetInt32("UserId");
                if (!HasFirmWideCalendarAccess(HttpContext.Session.GetString("UserRole")) && existingEvent.AssignedToUserId != userId && existingEvent.CreatedByUserId != userId) return Forbid();

                existingEvent.EndDateTime = existingEvent.ClientId.HasValue
                    ? await _safeguards.ApplyDurationAsync(existingEvent.ClientId.Value, existingEvent.StartDateTime, newEnd) : newEnd;
                existingEvent.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }

        // DELETE EVENT
        [HttpDelete, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                var existingEvent = await _context.CalendarEvents.FindAsync(id);
                if (existingEvent == null)
                {
                    return Json(new { success = false });
                }

                var userId = HttpContext.Session.GetInt32("UserId");
                if (!HasFirmWideCalendarAccess(HttpContext.Session.GetString("UserRole")) && existingEvent.AssignedToUserId != userId && existingEvent.CreatedByUserId != userId) return Forbid();

                existingEvent.Status = EventStatus.Cancelled;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }


        // DELETE TASK
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                {
                    return Json(new { success = false, error = "Task not found" });
                }
                if (!CanManageTask(task)) return Forbid();

                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // COMPLETE EVENT
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteEvent(int id, string completionNotes)
        {
            try
            {
                var existingEvent = await _context.CalendarEvents.FindAsync(id);
                if (existingEvent == null)
                {
                    return Json(new { success = false, error = "Event not found" });
                }

                var userId = HttpContext.Session.GetInt32("UserId");
                if (!HasFirmWideCalendarAccess(HttpContext.Session.GetString("UserRole")) && existingEvent.AssignedToUserId != userId && existingEvent.CreatedByUserId != userId) return Forbid();

                existingEvent.Status = EventStatus.Completed;
                existingEvent.CompletionNotes = completionNotes;
                existingEvent.ActualEndTime = DateTime.Now;
                existingEvent.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // EVENT DETAILS
        public async Task<IActionResult> EventDetails(int id)
        {
            var calendarEvent = await _context.CalendarEvents
                .Include(e => e.Case)
                .Include(e => e.AssignedToUser)
                .Include(e => e.CreatedByUser)
                .Include(e => e.Client)
                .Include(e => e.Attendees)
                    .ThenInclude(a => a.User)
                .Include(e => e.Reminders)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (calendarEvent == null)
            {
                TempData["Error"] = "Event not found";
                return RedirectToAction("Index");
            }

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (!HasFirmWideCalendarAccess(HttpContext.Session.GetString("UserRole")) && calendarEvent.AssignedToUserId != currentUserId && calendarEvent.CreatedByUserId != currentUserId) return Forbid();

            ViewBag.Interpreter = await _context.AppointmentInterpreterAssignments.SingleOrDefaultAsync(x => x.CalendarEventId == id);
            ViewBag.SupportPerson = await _context.AppointmentSupportPersonAssignments.SingleOrDefaultAsync(x => x.CalendarEventId == id);
            ViewBag.ClientFlags = calendarEvent.ClientId.HasValue ? await _safeguards.ActiveFlagsAsync(calendarEvent.ClientId.Value) : [];
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.CurrentUserId = HttpContext.Session.GetInt32("UserId");
            return View(calendarEvent);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignInterpreter(int eventId, string name, string language, string? contact)
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(eventId); if (calendarEvent is null) return NotFound();
            if (!CanManageEvent(calendarEvent)) return Forbid();
            await _safeguards.AssignInterpreterAsync(eventId, HttpContext.Session.GetInt32("UserId")!.Value, name, language, contact);
            TempData["Success"] = "Interpreter assigned. The appointment may now be confirmed if all safeguards are satisfied.";
            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSupportPerson(int eventId, string name, string? relationship)
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(eventId); if (calendarEvent is null) return NotFound();
            if (!CanManageEvent(calendarEvent)) return Forbid();
            await _safeguards.AssignSupportPersonAsync(eventId, HttpContext.Session.GetInt32("UserId")!.Value, name, relationship);
            TempData["Success"] = "Support person recorded for this appointment.";
            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }


        // POST: Calendar/GetSharedEvents - Get events for multiple users
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GetSharedEvents([FromBody] SharedEventsRequest request)
        {
            if (!HasFirmWideCalendarAccess(HttpContext.Session.GetString("UserRole"))) return Forbid();
            if (request?.UserIds == null || !request.UserIds.Any())
            {
                return Json(new List<object>());
            }

            var start = DateTime.Parse(request.Start);
            var end = DateTime.Parse(request.End);

            var events = await _context.CalendarEvents
                .Include(e => e.AssignedToUser)
                .Where(e => request.UserIds.Contains(e.AssignedToUserId.Value)
                            && e.StartDateTime >= start
                            && e.EndDateTime <= end
                            && e.Status != EventStatus.Cancelled)
                .Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    assignedTo = e.AssignedToUser.FullName,
                    start = e.StartDateTime.ToString("o"),
                    end = e.EndDateTime.ToString("o"),
                    color = e.Color,
                    allDay = e.IsAllDay,
                    type = e.Type.ToString()
                })
                .ToListAsync();

            return Json(events);
        }

        // SharedEventsRequest Model (add to your controller file)
        public class SharedEventsRequest
        {
            public List<int> UserIds { get; set; }
            public string Start { get; set; }
            public string End { get; set; }
        }

        // ADD EVENT ATTENDEE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttendee(int eventId, int userId, bool isOptional = false)
        {
            try
            {
                var calendarEvent = await _context.CalendarEvents.FindAsync(eventId); if (calendarEvent is null) return NotFound();
                if (!CanManageEvent(calendarEvent)) return Forbid();
                var existingAttendee = await _context.EventAttendees
                    .FirstOrDefaultAsync(a => a.EventId == eventId && a.UserId == userId);

                if (existingAttendee != null)
                {
                    return Json(new { success = false, error = "User is already an attendee" });
                }

                var attendee = new EventAttendee
                {
                    EventId = eventId,
                    UserId = userId,
                    ResponseStatus = "Pending",
                    InvitedAt = DateTime.Now,
                    IsOptional = isOptional
                };

                _context.EventAttendees.Add(attendee);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // UPDATE ATTENDEE RESPONSE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttendeeResponse(int eventId, string response, string comments = null)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var attendee = await _context.EventAttendees
                    .FirstOrDefaultAsync(a => a.EventId == eventId && a.UserId == userId);

                if (attendee == null)
                {
                    return Json(new { success = false, error = "Attendee not found" });
                }

                attendee.ResponseStatus = response;
                attendee.ResponseDate = DateTime.Now;
                attendee.Comments = comments;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // TASKS VIEW
        // TASKS VIEW
        public async Task<IActionResult> Tasks(string status = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            var isLawyer = userRole?.Equals("Lawyer", StringComparison.OrdinalIgnoreCase) == true;
            var activeUsers = await _context.Users.Where(u => u.IsActive && u.Role == UserRole.Lawyer).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.ActiveUsers = activeUsers;

            var caseQuery = _context.Cases.Where(c => c.Status != CaseStatus.Closed);
            if (isLawyer) caseQuery = caseQuery.Where(c => c.LawyerId == userId);
            var cases = await caseQuery.OrderBy(c => c.Title).ToListAsync();
            var taskClientIds = cases.Select(c => c.ClientId).Distinct().ToList();
            var taskClientNames = await _context.Clients.Where(c => taskClientIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.IsBusiness ? c.CompanyName! : (c.FirstName + " " + c.LastName));
            ViewBag.Cases = new SelectList(cases.Select(c => new { c.Id, Label = $"{c.Title} — {taskClientNames.GetValueOrDefault(c.ClientId, "Client")}" }), "Id", "Label");

            var retainers = await _context.Retainers.Where(r => r.Status == RetainerStatus.Active).ToListAsync();
            ViewBag.Retainers = new SelectList(retainers, "Id", "Title");

            var query = _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Case)
                .Include(t => t.Retainer)
                .OrderByDescending(t => t.CreatedAt)
                .AsQueryable();

            if (!HasFirmWideCalendarAccess(userRole))
            {
                query = query.Where(t => t.AssignedToId == userId || t.CreatedById == userId);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TaskItemStatus>(status, true, out var statusEnum))
            {
                query = query.Where(t => t.Status == statusEnum);
            }

            var tasks = await query.ToListAsync();
            ViewBag.StatusOptions = Enum.GetValues(typeof(TaskItemStatus));
            ViewBag.PriorityOptions = Enum.GetValues(typeof(TaskPriority));

            return View(tasks);
        }

        // CREATE TASK
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTask([FromBody] TaskItem task)
        {
            try
            {
                if (task == null)
                {
                    return Json(new { success = false, error = "Task data is null" });
                }

                if (string.IsNullOrWhiteSpace(task.Title))
                {
                    return Json(new { success = false, error = "Task title is required" });
                }

                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, error = "User not logged in" });
                }

                var role = HttpContext.Session.GetString("UserRole");
                var isLawyer = role?.Equals("Lawyer", StringComparison.OrdinalIgnoreCase) == true;
                if (isLawyer) task.AssignedToId = userId;
                if (!task.CaseId.HasValue) return Json(new { success = false, error = "Please select the related case." });
                var matter = await _context.Cases.SingleOrDefaultAsync(x => x.Id == task.CaseId && x.Status != CaseStatus.Closed);
                if (matter is null) return Json(new { success = false, error = "The selected case was not found." });
                if (isLawyer && matter.LawyerId != userId) return Forbid();
                if (!isLawyer && (!task.AssignedToId.HasValue || matter.LawyerId != task.AssignedToId))
                    return Json(new { success = false, error = "Select the lawyer assigned to this case." });

                // ⭐ CRITICAL FIX - Set all string fields to avoid NULL
                task.Title = task.Title?.Trim() ?? "";
                task.Description = task.Description ?? "";
                task.CreatedById = userId;
                task.CreatedAt = DateTime.Now;
                task.Status = TaskItemStatus.NotStarted;
                task.UpdatedAt = DateTime.Now;

                // Ensure foreign keys are properly set
                if (task.AssignedToId == 0) task.AssignedToId = null;
                if (task.CaseId == 0) task.CaseId = null;
                if (task.RetainerId == 0) task.RetainerId = null;

                // Set default values for nullable fields
                if (task.EstimatedHours == null) task.EstimatedHours = 0;
                if (task.ActualHours == null) task.ActualHours = 0;

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                return Json(new { success = true, taskId = task.Id });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.InnerException?.Message ?? dbEx.InnerException?.Message ?? dbEx.Message;
                Console.WriteLine($"DB Error: {innerMessage}");
                return Json(new { success = false, error = $"Database error: {innerMessage}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // UPDATE TASK
        [HttpPut, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskItem taskData)
        {
            try
            {
                var existingTask = await _context.Tasks.FindAsync(id);
                if (existingTask == null)
                {
                    return Json(new { success = false, error = "Task not found" });
                }

                var userId = HttpContext.Session.GetInt32("UserId");
                var role = HttpContext.Session.GetString("UserRole");
                if (!HasFirmWideCalendarAccess(role) && existingTask.AssignedToId != userId && existingTask.CreatedById != userId) return Forbid();
                var isLawyer = role?.Equals("Lawyer", StringComparison.OrdinalIgnoreCase) == true;
                if (isLawyer) taskData.AssignedToId = userId;
                if (!taskData.CaseId.HasValue || !taskData.AssignedToId.HasValue)
                    return Json(new { success = false, error = "A lawyer and related case are required." });
                if (!await _context.Cases.AnyAsync(x => x.Id == taskData.CaseId && x.Status != CaseStatus.Closed && x.LawyerId == taskData.AssignedToId))
                    return Json(new { success = false, error = "The selected case is not assigned to that lawyer." });

                // ⭐ Set all string fields to avoid NULL
                existingTask.Title = taskData.Title ?? existingTask.Title;
                existingTask.Description = taskData.Description ?? "";
                existingTask.Priority = taskData.Priority;
                existingTask.DueDate = taskData.DueDate;
                existingTask.AssignedToId = taskData.AssignedToId == 0 ? null : taskData.AssignedToId;
                existingTask.CaseId = taskData.CaseId == 0 ? null : taskData.CaseId;
                existingTask.RetainerId = taskData.RetainerId == 0 ? null : taskData.RetainerId;
                existingTask.EstimatedHours = taskData.EstimatedHours;
                existingTask.IsBillable = taskData.IsBillable;
                existingTask.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.InnerException?.Message ?? dbEx.InnerException?.Message ?? dbEx.Message;
                return Json(new { success = false, error = $"Database error: {innerMessage}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // UPDATE TASK STATUS
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTaskStatus(int id, TaskItemStatus status)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                {
                    return Json(new { success = false });
                }
                var userId = HttpContext.Session.GetInt32("UserId");
                var role = HttpContext.Session.GetString("UserRole");
                if (!HasFirmWideCalendarAccess(role) && task.AssignedToId != userId && task.CreatedById != userId) return Forbid();

                task.Status = status;
                if (status == TaskItemStatus.Completed)
                {
                    task.CompletedAt = DateTime.Now;
                }
                task.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }

        // UPDATE TASK HOURS
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTaskHours(int id, decimal actualHours)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                {
                    return Json(new { success = false, error = "Task not found" });
                }
                var userId = HttpContext.Session.GetInt32("UserId");
                var role = HttpContext.Session.GetString("UserRole");
                if (!HasFirmWideCalendarAccess(role) && task.AssignedToId != userId && task.CreatedById != userId) return Forbid();
                if (actualHours < 0) return Json(new { success = false, error = "Actual hours cannot be negative." });

                task.ActualHours = actualHours;
                task.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // TASK DETAILS
        public async Task<IActionResult> TaskDetails(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .Include(t => t.Case)
                .Include(t => t.Retainer)
                .Include(t => t.ParentTask)
                .Include(t => t.SubTasks)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                TempData["Error"] = "Task not found";
                return RedirectToAction("Tasks");
            }
            if (!CanManageTask(task)) return Forbid();

            return View(task);
        }

        // ADD TASK COMMENT
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTaskComment(int taskId, string comment, bool isInternal = false)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId); if (task is null) return NotFound();
                if (!CanManageTask(task)) return Forbid();
                if (string.IsNullOrWhiteSpace(comment)) return Json(new { success = false, error = "Enter a comment." });
                var taskComment = new TaskComment
                {
                    TaskId = taskId,
                    UserId = HttpContext.Session.GetInt32("UserId").Value,
                    Comment = comment,
                    CreatedAt = DateTime.Now,
                    IsInternal = isInternal
                };

                _context.TaskComments.Add(taskComment);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // CREATE SUBTASK
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubtask(int parentTaskId, string title, string description)
        {
            try
            {
                var parentTask = await _context.Tasks.FindAsync(parentTaskId);
                if (parentTask == null)
                {
                    return Json(new { success = false, error = "Parent task not found" });
                }
                if (!CanManageTask(parentTask)) return Forbid();
                if (string.IsNullOrWhiteSpace(title)) return Json(new { success = false, error = "Enter a subtask title." });

                var subtask = new TaskItem
                {
                    Title = title,
                    Description = description,
                    ParentTaskId = parentTaskId,
                    CreatedById = HttpContext.Session.GetInt32("UserId"),
                    CreatedAt = DateTime.Now,
                    Status = TaskItemStatus.NotStarted,
                    Priority = parentTask.Priority,
                    AssignedToId = parentTask.AssignedToId,
                    CaseId = parentTask.CaseId,
                    RetainerId = parentTask.RetainerId
                };

                _context.Tasks.Add(subtask);
                await _context.SaveChangesAsync();

                return Json(new { success = true, subtaskId = subtask.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // SHARED CALENDAR (Teams-like)
        public async Task<IActionResult> SharedCalendar(int userId)
        {
            if (!HasFirmWideCalendarAccess(HttpContext.Session.GetString("UserRole"))) return Forbid();
            var sharedUser = await _context.Users.FindAsync(userId);
            if (sharedUser == null)
            {
                TempData["Error"] = "User not found";
                return RedirectToAction("Index");
            }

            var events = await _context.CalendarEvents
                .Where(e => e.AssignedToUserId == userId && e.Status != EventStatus.Cancelled)
                .ToListAsync();

            ViewBag.SharedUser = sharedUser;

            var eventsJson = events.Select(e => new
            {
                id = e.Id,
                title = e.Title,
                start = e.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = e.EndDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                color = e.Color
            });

            ViewBag.EventsJson = JsonConvert.SerializeObject(eventsJson);

            return View();
        }

        // MEETING SCHEDULER (Find available times)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> FindAvailableTimes([FromBody] MeetingRequest request)
        {
            try
            {
                if (request == null || request.UserIds == null || !request.UserIds.Any())
                {
                    return Json(new List<DateTime>());
                }

                var users = await _context.Users
                    .Where(u => request.UserIds.Contains(u.Id))
                    .ToListAsync();

                var availableSlots = new List<DateTime>();
                var currentSlot = request.StartDate;

                while (currentSlot <= request.EndDate && availableSlots.Count < 10)
                {
                    bool allAvailable = true;

                    foreach (var user in users)
                    {
                        var hasEvent = await _context.CalendarEvents
                            .AnyAsync(e => e.AssignedToUserId == user.Id &&
                                          e.StartDateTime <= currentSlot.AddMinutes(request.DurationMinutes) &&
                                          e.EndDateTime >= currentSlot &&
                                          e.Status != EventStatus.Cancelled);

                        if (hasEvent)
                        {
                            allAvailable = false;
                            break;
                        }
                    }

                    if (allAvailable)
                    {
                        availableSlots.Add(currentSlot);
                    }

                    currentSlot = currentSlot.AddMinutes(30);
                }

                return Json(availableSlots);
            }
            catch (Exception)
            {
                return Json(new List<DateTime>());
            }
        }

        // GET UPCOMING EVENTS FOR DASHBOARD
        [HttpGet]
        public async Task<IActionResult> GetUpcomingEvents(int count = 5)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            var query = _context.CalendarEvents
                .Where(e => e.StartDateTime >= DateTime.Now && e.Status != EventStatus.Cancelled)
                .OrderBy(e => e.StartDateTime)
                .Take(count);

            if (userRole != "Admin")
            {
                query = query.Where(e => e.AssignedToUserId == userId || e.CreatedByUserId == userId);
            }

            var events = await query
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    StartTime = e.StartDateTime.ToString("dd MMM yyyy HH:mm"),
                    e.Color,
                    Type = e.Type.ToString()
                })
                .ToListAsync();

            return Json(events);
        }

        // GET TASK SUMMARY FOR DASHBOARD
        [HttpGet]
        public async Task<IActionResult> GetTaskSummary()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            var query = _context.Tasks.AsQueryable();

            if (userRole != "Admin")
            {
                query = query.Where(t => t.AssignedToId == userId || t.CreatedById == userId);
            }

            var summary = new
            {
                Total = await query.CountAsync(),
                NotStarted = await query.CountAsync(t => t.Status == TaskItemStatus.NotStarted),
                InProgress = await query.CountAsync(t => t.Status == TaskItemStatus.InProgress),
                OnHold = await query.CountAsync(t => t.Status == TaskItemStatus.OnHold),
                Completed = await query.CountAsync(t => t.Status == TaskItemStatus.Completed),
                Overdue = await query.CountAsync(t => t.DueDate < DateTime.Now && t.Status != TaskItemStatus.Completed),
                HighPriority = await query.CountAsync(t => t.Priority == TaskPriority.High && t.Status != TaskItemStatus.Completed),
                Urgent = await query.CountAsync(t => t.Priority == TaskPriority.Urgent && t.Status != TaskItemStatus.Completed)
            };

            return Json(summary);
        }

        // EXPORT CALENDAR (iCal format)
        [HttpGet]
        public async Task<IActionResult> ExportCalendar()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            var query = _context.CalendarEvents
                .Where(e => e.StartDateTime >= DateTime.Now.AddMonths(-1) && e.StartDateTime <= DateTime.Now.AddMonths(3));

            if (userRole != "Admin")
            {
                query = query.Where(e => e.AssignedToUserId == userId || e.CreatedByUserId == userId);
            }

            var events = await query.ToListAsync();

            var iCalContent = GenerateICalContent(events);
            var fileName = $"calendar_export_{DateTime.Now:yyyyMMdd}.ics";

            return File(System.Text.Encoding.UTF8.GetBytes(iCalContent), "text/calendar", fileName);
        }

        private string GenerateICalContent(List<CalendarEvent> events)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//Simplex Law Firm//Calendar//EN");
            sb.AppendLine("CALSCALE:GREGORIAN");

            foreach (var e in events)
            {
                sb.AppendLine("BEGIN:VEVENT");
                sb.AppendLine($"UID:{e.Id}@simplexlawfirm.com");
                sb.AppendLine($"DTSTAMP:{DateTime.Now:yyyyMMddTHHmmssZ}");
                sb.AppendLine($"DTSTART:{e.StartDateTime:yyyyMMddTHHmmssZ}");
                sb.AppendLine($"DTEND:{e.EndDateTime:yyyyMMddTHHmmssZ}");
                sb.AppendLine($"SUMMARY:{EscapeICalText(e.Title)}");
                if (!string.IsNullOrEmpty(e.Description))
                    sb.AppendLine($"DESCRIPTION:{EscapeICalText(e.Description)}");
                if (!string.IsNullOrEmpty(e.Location))
                    sb.AppendLine($"LOCATION:{EscapeICalText(e.Location)}");
                sb.AppendLine($"STATUS:{e.Status.ToString().ToUpper()}");
                sb.AppendLine("END:VEVENT");
            }

            sb.AppendLine("END:VCALENDAR");
            return sb.ToString();
        }

        private string EscapeICalText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("\\", "\\\\")
                      .Replace("\n", "\\n")
                      .Replace(",", "\\,")
                      .Replace(";", "\\;");
        }

        private bool CanManageTask(TaskItem task)
        {
            if (HasFirmWideCalendarAccess(HttpContext.Session.GetString("UserRole"))) return true;
            var userId = HttpContext.Session.GetInt32("UserId");
            return userId.HasValue && (task.AssignedToId == userId || task.CreatedById == userId);
        }

        private bool CanManageEvent(CalendarEvent calendarEvent)
        {
            if (HasFirmWideCalendarAccess(HttpContext.Session.GetString("UserRole"))) return true;
            var userId = HttpContext.Session.GetInt32("UserId");
            return userId.HasValue && (calendarEvent.AssignedToUserId == userId || calendarEvent.CreatedByUserId == userId);
        }
    }

    // Meeting Request Model
    public class MeetingRequest
    {
        public List<int> UserIds { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationMinutes { get; set; }
    }
}
