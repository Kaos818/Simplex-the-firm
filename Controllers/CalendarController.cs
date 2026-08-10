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
    [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
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
        public async Task<IActionResult> Index(string view = "month")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            // Get active users for dropdown
            var activeUsers = await _context.Users.Where(u => u.IsActive).ToListAsync();
            ViewBag.ActiveUsers = activeUsers;
            ViewBag.Clients = await _context.Clients.Where(c => c.IsActive).OrderBy(c => c.FirstName).ThenBy(c => c.LastName).ToListAsync();
            ViewBag.Retainers = await _context.Retainers.Where(r => r.Status == RetainerStatus.Active).OrderBy(r => r.Title).ToListAsync();

            // Get cases for dropdown
            var cases = await _context.Cases.Where(c => c.Status != CaseStatus.Closed).ToListAsync();
            ViewBag.Cases = new SelectList(cases, "Id", "Title");

            ViewBag.CurrentView = view;
            ViewBag.UserRole = userRole;

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

            var eventsJson = events.Select(e => new
            {
                id = e.Id,
                title = e.Title,
                start = e.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = e.EndDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                allDay = e.IsAllDay,
                color = string.IsNullOrWhiteSpace(e.Color) ? ColorFor(e.Type) : e.Color,
                type = e.Type.ToString(),
                status = e.Status.ToString(),
                clientName = e.Client?.FullName,
                assignedTo = e.AssignedToUser == null ? "Unassigned" : e.AssignedToUser.FullName,
                location = e.Location
            });

            ViewBag.EventsJson = Newtonsoft.Json.JsonConvert.SerializeObject(eventsJson);

            return View();
        }

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
                .Where(e => e.StartDateTime >= start && e.EndDateTime <= end);

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
                color = string.IsNullOrWhiteSpace(e.Color) ? ColorFor(e.Type) : e.Color,
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

                if (request.EndLocal <= request.StartLocal) return Json(new { success = false, error = "End time must be after start time." });
                var clientFacing = request.Type is EventType.Appointment or EventType.Consultation;
                if (clientFacing && (request.ClientId is null || request.AppointmentFee is null or <= 0 || (string.IsNullOrWhiteSpace(request.Location) && string.IsNullOrWhiteSpace(request.MeetingLink)) || request.PaymentDueDays <= 0))
                    return Json(new { success = false, error = "Client, positive fee, valid times, payment terms, and a location or meeting link are required." });
                var client = clientFacing ? await _context.Clients.FindAsync(request.ClientId) : null;
                if (clientFacing && client is null) return Json(new { success = false, error = "Client was not found." });
                if (clientFacing && request.CaseId.HasValue && !await _context.Cases.AnyAsync(x => x.Id == request.CaseId && x.ClientId == request.ClientId))
                    return Json(new { success = false, error = "The selected matter does not belong to this client." });
                if (request.RetainerId is not null && !await _context.Retainers.AnyAsync(r => r.Id == request.RetainerId && r.ClientId == request.ClientId && r.Status == RetainerStatus.Active))
                    return Json(new { success = false, error = "The selected retainer is not active for this client." });
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var adjustedEnd = clientFacing
                    ? await _safeguards.ApplyDurationAsync(request.ClientId!.Value, request.StartLocal.ToUniversalTime(), request.EndLocal.ToUniversalTime())
                    : request.EndLocal.ToUniversalTime();
                var eventData = new CalendarEvent {
                    Title = request.Title.Trim(), Description = request.Description ?? "", Location = request.Location ?? "", MeetingLink = request.MeetingLink ?? "",
                    StartDateTime = request.StartLocal.ToUniversalTime(), EndDateTime = adjustedEnd, Type = request.Type,
                    AssignedToUserId = request.AssignedToUserId, CaseId = request.CaseId, RetainerId = request.RetainerId, ClientId = clientFacing ? request.ClientId : null,
                    AppointmentFee = clientFacing ? request.AppointmentFee : null, PaymentDueDays = request.PaymentDueDays,
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
                    var root = _configuration["Email:PublicBaseUrl"]?.TrimEnd('/');
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
                    var text = $"{eventData.Title}\nScheduled by: {staff?.FullName ?? creator?.FullName ?? "Simplex Law Firm"}\nDate: {startLocal:dddd, dd MMMM yyyy}\nTime: {startLocal:HH:mm} to {endLocal:HH:mm} (Africa/Johannesburg)\nLocation: {venue}\nMeeting link: {meeting}\nDescription: {description}\nFee: R{eventData.AppointmentFee:N2}\nPayment due within {eventData.PaymentDueDays} days. Late-payment terms: {penalty}.\nBilling occurs only after you accept and the appointment has ended.\nAccept: {accept}\nReject: {reject}";
                    var encoded = System.Net.WebUtility.HtmlEncode(text).Replace("\n", "<br>");
                    var html = $"<p>{encoded}</p><p><a style=\"display:inline-block;padding:10px 16px;background:#198754;color:white;text-decoration:none\" href=\"{System.Net.WebUtility.HtmlEncode(accept)}\">Accept appointment</a> &nbsp; <a style=\"display:inline-block;padding:10px 16px;background:#dc3545;color:white;text-decoration:none\" href=\"{System.Net.WebUtility.HtmlEncode(reject)}\">Reject appointment</a></p>";
                    await _email.QueueAsync(client.Email, $"Appointment invitation: {eventData.Title}", html, text, $"appointment-invite:{eventData.Id}", HttpContext.RequestAborted);
                    var clientUser = await _context.Users.SingleOrDefaultAsync(x => x.Role == UserRole.Client && x.Email.ToUpper() == client.Email.ToUpper());
                    if (clientUser is not null)
                        await _notifications.QueueAsync(clientUser.Id, "AppointmentInvitation", "New appointment invitation",
                            $"{eventData.Title} is scheduled for {startLocal:dd MMM yyyy HH:mm}.", $"/Calendar/EventDetails/{eventData.Id}", $"appointment-invite-notification:{eventData.Id}", HttpContext.RequestAborted);
                    _context.AuditEntries.Add(new() { ActorUserId = userId, EntityType = "CalendarEvent", EntityId = eventData.Id.ToString(), Action = "Appointment created" });
                    _context.AuditEntries.Add(new() { ActorUserId = userId, EntityType = "CalendarEvent", EntityId = eventData.Id.ToString(), Action = "Invitation sent" });
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

        // UPDATE EVENT
        [HttpPut, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] CalendarEvent eventData)
        {
            try
            {
                var existingEvent = await _context.CalendarEvents.FindAsync(id);
                if (existingEvent == null)
                {
                    return Json(new { success = false, error = "Event not found" });
                }

                existingEvent.Title = eventData.Title;
                existingEvent.Description = eventData.Description ?? "";
                existingEvent.Location = eventData.Location ?? "";
                existingEvent.StartDateTime = eventData.StartDateTime;
                existingEvent.EndDateTime = existingEvent.ClientId.HasValue
                    ? await _safeguards.ApplyDurationAsync(existingEvent.ClientId.Value, eventData.StartDateTime, eventData.EndDateTime)
                    : eventData.EndDateTime;
                existingEvent.IsAllDay = eventData.IsAllDay;
                existingEvent.Type = eventData.Type;
                existingEvent.Color = eventData.Color;
                existingEvent.MeetingLink = eventData.MeetingLink ?? "";
                existingEvent.CompletionNotes = eventData.CompletionNotes ?? "";
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

            return Json(new
            {
                id = calendarEvent.Id,
                title = calendarEvent.Title,
                type = calendarEvent.Type.ToString(),
                start = calendarEvent.StartDateTime,
                end = calendarEvent.EndDateTime,
                location = calendarEvent.Location,
                description = calendarEvent.Description,
                assignedToUserId = calendarEvent.AssignedToUserId,
                caseId = calendarEvent.CaseId,
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
                .Include(e => e.Attendees)
                    .ThenInclude(a => a.User)
                .Include(e => e.Reminders)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (calendarEvent == null)
            {
                TempData["Error"] = "Event not found";
                return RedirectToAction("Index");
            }

            ViewBag.Interpreter = await _context.AppointmentInterpreterAssignments.SingleOrDefaultAsync(x => x.CalendarEventId == id);
            ViewBag.SupportPerson = await _context.AppointmentSupportPersonAssignments.SingleOrDefaultAsync(x => x.CalendarEventId == id);
            ViewBag.ClientFlags = calendarEvent.ClientId.HasValue ? await _safeguards.ActiveFlagsAsync(calendarEvent.ClientId.Value) : [];
            return View(calendarEvent);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignInterpreter(int eventId, string name, string language, string? contact)
        {
            await _safeguards.AssignInterpreterAsync(eventId, HttpContext.Session.GetInt32("UserId")!.Value, name, language, contact);
            TempData["Success"] = "Interpreter assigned. The appointment may now be confirmed if all safeguards are satisfied.";
            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSupportPerson(int eventId, string name, string? relationship)
        {
            await _safeguards.AssignSupportPersonAsync(eventId, HttpContext.Session.GetInt32("UserId")!.Value, name, relationship);
            TempData["Success"] = "Support person recorded for this appointment.";
            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }


        // POST: Calendar/GetSharedEvents - Get events for multiple users
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GetSharedEvents([FromBody] SharedEventsRequest request)
        {
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

            // Populate dropdowns
            var activeUsers = await _context.Users.Where(u => u.IsActive).ToListAsync();
            ViewBag.ActiveUsers = activeUsers;

            var cases = await _context.Cases.Where(c => c.Status != CaseStatus.Closed).ToListAsync();
            ViewBag.Cases = new SelectList(cases, "Id", "Title");

            var retainers = await _context.Retainers.Where(r => r.Status == RetainerStatus.Active).ToListAsync();
            ViewBag.Retainers = new SelectList(retainers, "Id", "Title");

            var query = _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Case)
                .Include(t => t.Retainer)
                .OrderByDescending(t => t.CreatedAt)
                .AsQueryable();

            if (userRole != "Admin")
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

            return View(task);
        }

        // ADD TASK COMMENT
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTaskComment(int taskId, string comment, bool isInternal = false)
        {
            try
            {
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
