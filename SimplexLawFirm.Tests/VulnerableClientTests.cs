using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Services.CurrentUser;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Notifications;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class VulnerableClientTests
{
    [Fact]
    public async Task Assigned_attorney_can_raise_flag_and_unassigned_attorney_cannot()
    {
        await using var f = await Fixture.CreateAsync();
        var flag = await f.Service.RaiseAsync(f.Client.Id, f.Lawyer.Id, ClientSafeguard.Interpreter, "Client communicates in isiZulu.", "isiZulu");
        Assert.Equal(VulnerableFlagStatus.PendingReview, flag.Status);
        Assert.Equal(DateTime.UtcNow.AddDays(2).Date, flag.ReviewDueAtUtc.Date);
        Assert.Single(f.Notifications.Items);
        Assert.Contains(await f.Db.AuditEntries.ToListAsync(), x => x.Action == "Support flag raised");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            f.Service.RaiseAsync(f.Client.Id, f.OtherLawyer.Id, ClientSafeguard.ExtendedMeetingTime, "Needs more time.", null));
    }

    [Fact]
    public async Task Interpreter_flag_blocks_confirmation_until_assignment_exists()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service.RaiseAsync(f.Client.Id, f.Lawyer.Id, ClientSafeguard.Interpreter, "Interpreter required.", "Sesotho");
        var appointment = await f.Appointment();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.EnsureAppointmentMayConfirmAsync(appointment.Id));
        await f.Service.AssignInterpreterAsync(appointment.Id, f.Lawyer.Id, "Interpreter One", "Sesotho", "012345");
        await f.Service.EnsureAppointmentMayConfirmAsync(appointment.Id);
        Assert.Single(await f.Db.AppointmentInterpreterAssignments.ToListAsync());
    }

    [Fact]
    public async Task Extended_time_enforces_ninety_minute_minimum()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service.RaiseAsync(f.Client.Id, f.Lawyer.Id, ClientSafeguard.ExtendedMeetingTime, "Needs slower explanations.", null);
        var start = DateTime.UtcNow;
        Assert.Equal(start.AddMinutes(90), await f.Service.ApplyDurationAsync(f.Client.Id, start, start.AddMinutes(45)));
        Assert.Equal(start.AddMinutes(120), await f.Service.ApplyDurationAsync(f.Client.Id, start, start.AddMinutes(120)));
    }

    [Fact]
    public async Task Support_person_blocks_appointment_and_self_service_until_satisfied()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service.RaiseAsync(f.Client.Id, f.Lawyer.Id, ClientSafeguard.SupportPerson, "Client must not act unaccompanied.", null);
        var appointment = await f.Appointment();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.EnsureAppointmentMayConfirmAsync(appointment.Id));
        Assert.False(await f.Service.HasActiveSupportSessionAsync(f.Client.Id));
        await f.Service.AssignSupportPersonAsync(appointment.Id, f.Lawyer.Id, "Trusted Person", "Sibling");
        await f.Service.EnsureAppointmentMayConfirmAsync(appointment.Id);
        var session = await f.Service.OpenSupportSessionAsync(f.Client.Id, f.Lawyer.Id, "Trusted Person", "Review and submit retainer request");
        Assert.True(await f.Service.HasActiveSupportSessionAsync(f.Client.Id));
        Assert.True(session.ExpiresAtUtc > DateTime.UtcNow);
        var flag = await f.Db.VulnerableClientFlags.SingleAsync();
        await f.Service.ReviewAsync(flag.Id, f.Director.Id, true, "Confirmed; supported activity must be re-authorised.");
        Assert.False(await f.Service.HasActiveSupportSessionAsync(f.Client.Id));
    }

    [Fact]
    public async Task Every_staff_member_must_acknowledge_each_changed_flag_on_each_matter()
    {
        await using var f = await Fixture.CreateAsync();
        var flag = await f.Service.RaiseAsync(f.Client.Id, f.Lawyer.Id, ClientSafeguard.ExtendedMeetingTime, "Extra time.", null);
        Assert.Single(await f.Service.UnacknowledgedAsync(f.Matter.Id, f.Lawyer.Id));
        await f.Service.AcknowledgeAsync(f.Matter.Id, f.Lawyer.Id);
        Assert.Empty(await f.Service.UnacknowledgedAsync(f.Matter.Id, f.Lawyer.Id));
        Assert.Single(await f.Service.UnacknowledgedAsync(f.Matter.Id, f.Director.Id));
        await f.Service.ReviewAsync(flag.Id, f.Director.Id, true, "Accommodation remains necessary.");
        Assert.Single(await f.Service.UnacknowledgedAsync(f.Matter.Id, f.Lawyer.Id));
        Assert.Contains(await f.Db.AuditEntries.ToListAsync(), x => x.Action.Contains("acknowledged"));
    }

    [Fact]
    public async Task Overdue_initial_review_escalates_and_periodic_review_returns_to_pending()
    {
        await using var f = await Fixture.CreateAsync();
        var flag = await f.Service.RaiseAsync(f.Client.Id, f.Lawyer.Id, ClientSafeguard.ExtendedMeetingTime, "Extra time.", null);
        flag.ReviewDueAtUtc = DateTime.UtcNow.AddSeconds(-1); await f.Db.SaveChangesAsync();
        await f.Service.RunGovernanceAsync();
        Assert.Equal(VulnerableFlagStatus.Escalated, flag.Status);
        await f.Service.ReviewAsync(flag.Id, f.Director.Id, true, "Confirmed after escalation.");
        flag.NextReviewAtUtc = DateTime.UtcNow.AddSeconds(-1); await f.Db.SaveChangesAsync();
        await f.Service.RunGovernanceAsync();
        Assert.Equal(VulnerableFlagStatus.PendingReview, flag.Status);
        Assert.True(flag.ReviewDueAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Director_removal_stops_enforcement_and_is_audited()
    {
        await using var f = await Fixture.CreateAsync();
        var flag = await f.Service.RaiseAsync(f.Client.Id, f.Lawyer.Id, ClientSafeguard.Interpreter, "Temporary language need.", "French");
        await f.Service.ReviewAsync(flag.Id, f.Director.Id, false, "Client confirmed interpreter is no longer required.");
        Assert.Equal(VulnerableFlagStatus.Removed, flag.Status);
        var appointment = await f.Appointment();
        await f.Service.EnsureAppointmentMayConfirmAsync(appointment.Id);
        Assert.Contains(await f.Db.AuditEntries.ToListAsync(), x => x.Action == "Support flag removed");
    }

    [Fact]
    public async Task Client_mutation_middleware_blocks_unaccompanied_action_and_allows_supported_session()
    {
        await using var f = await Fixture.CreateAsync();
        var clientUser = new ApplicationUser { FullName = "Client User", Email = f.Client.Email, PasswordHash = "x", Role = UserRole.Client, IsActive = true };
        f.Db.Users.Add(clientUser); await f.Db.SaveChangesAsync();
        await f.Service.RaiseAsync(f.Client.Id, f.Lawyer.Id, ClientSafeguard.SupportPerson, "Must be accompanied.", null);
        var called = false;
        var middleware = new SupportPersonEnforcementMiddleware(_ => { called = true; return Task.CompletedTask; });
        var first = Context(clientUser.Id);
        var accessor = new HttpContextAccessor { HttpContext = first };
        await middleware.InvokeAsync(first, new CurrentClientService(accessor, f.Db), f.Service, f.Db);
        Assert.False(called);
        Assert.Equal("/VulnerableClient/SupportRequired", first.Response.Headers.Location);

        await f.Service.OpenSupportSessionAsync(f.Client.Id, f.Lawyer.Id, "Trusted Person", "Submit instructed request");
        called = false;
        var second = Context(clientUser.Id); accessor.HttpContext = second;
        await middleware.InvokeAsync(second, new CurrentClientService(accessor, f.Db), f.Service, f.Db);
        Assert.True(called);
    }

    [Fact]
    public async Task Direct_matter_route_is_blocked_until_staff_acknowledges_flags()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service.RaiseAsync(f.Client.Id, f.Lawyer.Id, ClientSafeguard.ExtendedMeetingTime, "Allow additional consultation time.", null);
        var filter = new MatterSafeguardAcknowledgementFilter(f.Db, f.Service);
        var http = Context(f.Lawyer.Id); http.Session.SetString("UserRole", "Lawyer"); http.Request.Method = "GET";
        var route = new RouteData(); route.Values["controller"] = "Case"; route.Values["action"] = "Edit";
        var actionContext = new ActionContext(http, route, new ActionDescriptor());
        var executing = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?> { ["id"] = f.Matter.Id }, new object());
        var continued = false;
        await filter.OnActionExecutionAsync(executing, () => { continued = true; return Task.FromResult(new ActionExecutedContext(actionContext, [], new object())); });
        Assert.False(continued);
        Assert.IsType<RedirectResult>(executing.Result);
        await f.Service.AcknowledgeAsync(f.Matter.Id, f.Lawyer.Id);
        executing.Result = null; continued = false;
        await filter.OnActionExecutionAsync(executing, () => { continued = true; return Task.FromResult(new ActionExecutedContext(actionContext, [], new object())); });
        Assert.True(continued);
    }

    [Fact]
    public async Task Vulnerable_client_audit_records_are_append_only()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service.RaiseAsync(f.Client.Id, f.Lawyer.Id, ClientSafeguard.ExtendedMeetingTime, "Extra time.", null);
        var audit = await f.Db.AuditEntries.FirstAsync(x => x.EntityType == nameof(VulnerableClientFlag));
        audit.Action = "tampered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync());
    }

    private static DefaultHttpContext Context(int userId)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<ISessionFeature>(new SessionFeature { Session = new TestSession() });
        context.Session.SetInt32("UserId", userId); context.Session.SetString("UserRole", "Client");
        context.Request.Method = "POST"; context.Request.Path = "/Retainer/RequestService";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public FakeNotifications Notifications { get; } = new();
        public VulnerableClientService Service { get; }
        public Client Client { get; private set; } = null!;
        public ApplicationUser Lawyer { get; private set; } = null!;
        public ApplicationUser OtherLawyer { get; private set; } = null!;
        public ApplicationUser Director { get; private set; } = null!;
        public Case Matter { get; private set; } = null!;
        private Fixture(SqliteConnection connection, ApplicationDbContext db)
        {
            this.connection = connection; Db = db;
            Service = new(db, Notifications, Options.Create(new VulnerableClientOptions()));
        }
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var f = new Fixture(connection, db); await f.Seed(); return f;
        }
        private async Task Seed()
        {
            Client = new Client { FirstName = "Supported", LastName = "Client", Email = "supported@test", Phone = "1" };
            Lawyer = User("Assigned", UserRole.Lawyer); OtherLawyer = User("Other", UserRole.Lawyer); Director = User("Director", UserRole.Admin);
            Db.AddRange(Client, Lawyer, OtherLawyer, Director); await Db.SaveChangesAsync();
            Matter = new Case { CaseNumber = "VC-1", Title = "Supported matter", ClientId = Client.Id, LawyerId = Lawyer.Id,
                CaseType = "General", Status = CaseStatus.Active };
            Db.Cases.Add(Matter); await Db.SaveChangesAsync();
        }
        public async Task<CalendarEvent> Appointment()
        {
            var e = new CalendarEvent { Title = "Consultation", Description = "", Location = "Office", MeetingLink = "",
                StartDateTime = DateTime.UtcNow.AddDays(1), EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                ClientId = Client.Id, CaseId = Matter.Id, Type = EventType.Appointment, Status = EventStatus.Scheduled,
                Color = "", CompletionNotes = "", RecurrenceRule = "" };
            Db.CalendarEvents.Add(e); await Db.SaveChangesAsync(); return e;
        }
        private static ApplicationUser User(string name, UserRole role) => new()
            { FullName = name, Email = $"{name.ToLower()}@test", PasswordHash = "x", Role = role, IsActive = true };
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
    public sealed class FakeNotifications : INotificationService
    {
        public List<(int UserId, string Type)> Items { get; } = [];
        public Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? deduplicationKey, CancellationToken cancellationToken = default)
        { Items.Add((userId, type)); return Task.CompletedTask; }
    }
    private sealed class SessionFeature : ISessionFeature { public ISession Session { get; set; } = null!; }
    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> values = [];
        public bool IsAvailable => true; public string Id => "test"; public IEnumerable<string> Keys => values.Keys;
        public void Clear() => values.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => values.Remove(key);
        public void Set(string key, byte[] value) => values[key] = value;
        public bool TryGetValue(string key, out byte[] value) => values.TryGetValue(key, out value!);
    }
}
