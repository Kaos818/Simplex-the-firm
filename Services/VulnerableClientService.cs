using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;

namespace SimplexLawFirm.Services;

public sealed class VulnerableClientOptions
{
    public int DirectorReviewDays { get; set; } = 2;
    public int PeriodicReviewDays { get; set; } = 90;
    public int StandardMeetingMinutes { get; set; } = 60;
    public int ExtendedMeetingExtraMinutes { get; set; } = 30;
    public int SupportSessionHours { get; set; } = 4;
}

public interface IVulnerableClientService
{
    Task<VulnerableClientFlag> RaiseAsync(int clientId, int attorneyId, ClientSafeguard safeguard, string reason, string? language, CancellationToken ct = default);
    Task ReviewAsync(int flagId, int directorId, bool confirm, string note, CancellationToken ct = default);
    Task<IReadOnlyList<VulnerableClientFlag>> ActiveFlagsAsync(int clientId, CancellationToken ct = default);
    Task<IReadOnlyList<VulnerableClientFlag>> UnacknowledgedAsync(int caseId, int staffUserId, CancellationToken ct = default);
    Task AcknowledgeAsync(int caseId, int staffUserId, CancellationToken ct = default);
    Task<DateTime> ApplyDurationAsync(int clientId, DateTime startUtc, DateTime requestedEndUtc, CancellationToken ct = default);
    Task EnsureAppointmentMayConfirmAsync(int calendarEventId, CancellationToken ct = default);
    Task AssignInterpreterAsync(int eventId, int staffId, string name, string language, string? contact, CancellationToken ct = default);
    Task AssignSupportPersonAsync(int eventId, int staffId, string name, string? relationship, CancellationToken ct = default);
    Task<ClientSupportSession> OpenSupportSessionAsync(int clientId, int staffId, string name, string purpose, CancellationToken ct = default);
    Task<bool> HasActiveSupportSessionAsync(int clientId, CancellationToken ct = default);
    Task<int> RunGovernanceAsync(CancellationToken ct = default);
}

public sealed class VulnerableClientService(
    ApplicationDbContext db,
    INotificationService notifications,
    IOptions<VulnerableClientOptions> options) : IVulnerableClientService
{
    private static readonly VulnerableFlagStatus[] Enforced =
        [VulnerableFlagStatus.PendingReview, VulnerableFlagStatus.Confirmed, VulnerableFlagStatus.Escalated];

    public async Task<VulnerableClientFlag> RaiseAsync(int clientId, int attorneyId, ClientSafeguard safeguard, string reason, string? language, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("The reason for the support flag is required.");
        if (safeguard == ClientSafeguard.Interpreter && string.IsNullOrWhiteSpace(language))
            throw new InvalidOperationException("Record the language required for the interpreter.");
        if (!await db.Clients.AnyAsync(x => x.Id == clientId && x.IsActive, ct))
            throw new InvalidOperationException("The client is not active.");
        if (!await db.Cases.AnyAsync(x => x.ClientId == clientId && x.LawyerId == attorneyId && x.Status != CaseStatus.Archived, ct))
            throw new UnauthorizedAccessException("Only an attorney assigned to one of this client's matters may raise a support flag.");
        if (await db.VulnerableClientFlags.AnyAsync(x => x.ClientId == clientId && x.Safeguard == safeguard && Enforced.Contains(x.Status), ct))
            throw new InvalidOperationException("This safeguard already has an active flag.");
        var now = DateTime.UtcNow;
        var flag = new VulnerableClientFlag
        {
            ClientId = clientId, RaisedByAttorneyId = attorneyId, Safeguard = safeguard,
            Reason = reason.Trim(), LanguageRequired = language?.Trim(), RaisedAtUtc = now, LastChangedAtUtc = now,
            ReviewDueAtUtc = now.AddDays(Math.Max(1, options.Value.DirectorReviewDays))
        };
        db.VulnerableClientFlags.Add(flag);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            db.Entry(flag).State = EntityState.Detached;
            throw new InvalidOperationException("This safeguard already has an active flag.");
        }
        foreach (var director in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct))
            await notifications.QueueAsync(director, "VulnerableClientReview", "Client safeguard requires review",
                $"{safeguard} support was raised for client {clientId}. Review by {flag.ReviewDueAtUtc:d MMM yyyy}.",
                $"/VulnerableClient/Review/{flag.Id}", $"vulnerable-flag-raised-{flag.Id}-{director}", ct);
        Audit(attorneyId, flag, "Support flag raised");
        await db.SaveChangesAsync(ct);
        return flag;
    }

    public async Task ReviewAsync(int flagId, int directorId, bool confirm, string note, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(note)) throw new InvalidOperationException("A review reason is required.");
        var flag = await db.VulnerableClientFlags.SingleAsync(x => x.Id == flagId, ct);
        if (!Enforced.Contains(flag.Status)) throw new InvalidOperationException("This flag has already been removed.");
        var now = DateTime.UtcNow;
        flag.ReviewedByDirectorId = directorId; flag.ReviewedAtUtc = now; flag.ReviewNote = note.Trim(); flag.LastChangedAtUtc = now;
        if (confirm)
        {
            flag.Status = VulnerableFlagStatus.Confirmed;
            flag.NextReviewAtUtc = now.AddDays(Math.Max(1, options.Value.PeriodicReviewDays));
            flag.ReviewDueAtUtc = flag.NextReviewAtUtc.Value;
        }
        else
        {
            flag.Status = VulnerableFlagStatus.Removed; flag.RemovedAtUtc = now; flag.NextReviewAtUtc = null;
        }
        if (flag.Safeguard == ClientSafeguard.SupportPerson)
        {
            var activeSessions = await db.ClientSupportSessions
                .Where(x => x.ClientId == flag.ClientId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
                .ToListAsync(ct);
            foreach (var session in activeSessions) session.RevokedAtUtc = now;
        }
        Audit(directorId, flag, confirm ? "Support flag confirmed" : "Support flag removed");
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        { throw new InvalidOperationException("This flag changed while it was being reviewed. Reload it before making a decision."); }
    }

    public async Task<IReadOnlyList<VulnerableClientFlag>> ActiveFlagsAsync(int clientId, CancellationToken ct = default) =>
        await db.VulnerableClientFlags.Include(x => x.RaisedByAttorney)
            .Where(x => x.ClientId == clientId && Enforced.Contains(x.Status)).OrderBy(x => x.Safeguard).ToListAsync(ct);

    public async Task<IReadOnlyList<VulnerableClientFlag>> UnacknowledgedAsync(int caseId, int staffUserId, CancellationToken ct = default)
    {
        var clientId = await db.Cases.Where(x => x.Id == caseId).Select(x => (int?)x.ClientId).SingleOrDefaultAsync(ct);
        if (!clientId.HasValue) return [];
        var flags = await ActiveFlagsAsync(clientId.Value, ct);
        var acknowledged = await db.VulnerableFlagAcknowledgements.Where(x => x.CaseId == caseId && x.StaffUserId == staffUserId)
            .GroupBy(x => x.VulnerableClientFlagId).Select(x => new { FlagId = x.Key, At = x.Max(a => a.AcknowledgedAtUtc) }).ToListAsync(ct);
        return flags.Where(f => !acknowledged.Any(a => a.FlagId == f.Id && a.At >= f.LastChangedAtUtc)).ToList();
    }

    public async Task AcknowledgeAsync(int caseId, int staffUserId, CancellationToken ct = default)
    {
        var flags = await UnacknowledgedAsync(caseId, staffUserId, ct);
        foreach (var flag in flags)
        {
            db.VulnerableFlagAcknowledgements.Add(new()
                { CaseId = caseId, StaffUserId = staffUserId, VulnerableClientFlagId = flag.Id });
            Audit(staffUserId, flag, $"Safeguard acknowledged for matter {caseId}");
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<DateTime> ApplyDurationAsync(int clientId, DateTime startUtc, DateTime requestedEndUtc, CancellationToken ct = default)
    {
        var extended = await db.VulnerableClientFlags.AnyAsync(x => x.ClientId == clientId &&
            x.Safeguard == ClientSafeguard.ExtendedMeetingTime && Enforced.Contains(x.Status), ct);
        if (!extended) return requestedEndUtc;
        var minimum = startUtc.AddMinutes(Math.Max(1, options.Value.StandardMeetingMinutes) +
            Math.Max(1, options.Value.ExtendedMeetingExtraMinutes));
        return requestedEndUtc < minimum ? minimum : requestedEndUtc;
    }

    public async Task EnsureAppointmentMayConfirmAsync(int calendarEventId, CancellationToken ct = default)
    {
        var e = await db.CalendarEvents.SingleAsync(x => x.Id == calendarEventId, ct);
        if (!e.ClientId.HasValue) return;
        var flags = await ActiveFlagsAsync(e.ClientId.Value, ct);
        if (flags.Any(x => x.Safeguard == ClientSafeguard.Interpreter) &&
            !await db.AppointmentInterpreterAssignments.AnyAsync(x => x.CalendarEventId == e.Id, ct))
            throw new InvalidOperationException("This appointment cannot be confirmed until an interpreter has been assigned.");
        if (flags.Any(x => x.Safeguard == ClientSafeguard.SupportPerson) &&
            !await db.AppointmentSupportPersonAssignments.AnyAsync(x => x.CalendarEventId == e.Id, ct))
            throw new InvalidOperationException("This appointment cannot be confirmed until the required support person is recorded.");
    }

    public async Task AssignInterpreterAsync(int eventId, int staffId, string name, string language, string? contact, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(language))
            throw new InvalidOperationException("Interpreter name and language are required.");
        var assignment = await db.AppointmentInterpreterAssignments.SingleOrDefaultAsync(x => x.CalendarEventId == eventId, ct);
        if (assignment == null) db.AppointmentInterpreterAssignments.Add(new()
            { CalendarEventId = eventId, AssignedByUserId = staffId, InterpreterName = name.Trim(), Language = language.Trim(), ContactDetails = contact?.Trim() });
        else { assignment.InterpreterName = name.Trim(); assignment.Language = language.Trim(); assignment.ContactDetails = contact?.Trim(); assignment.AssignedByUserId = staffId; assignment.AssignedAtUtc = DateTime.UtcNow; }
        db.AuditEntries.Add(new AuditEntry { ActorUserId = staffId, EntityType = nameof(CalendarEvent), EntityId = eventId.ToString(), Action = "Interpreter assigned" });
        await db.SaveChangesAsync(ct);
    }

    public async Task AssignSupportPersonAsync(int eventId, int staffId, string name, string? relationship, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Support person name is required.");
        var assignment = await db.AppointmentSupportPersonAssignments.SingleOrDefaultAsync(x => x.CalendarEventId == eventId, ct);
        if (assignment == null) db.AppointmentSupportPersonAssignments.Add(new()
            { CalendarEventId = eventId, RecordedByUserId = staffId, SupportPersonName = name.Trim(), Relationship = relationship?.Trim() });
        else { assignment.SupportPersonName = name.Trim(); assignment.Relationship = relationship?.Trim(); assignment.RecordedByUserId = staffId; assignment.RecordedAtUtc = DateTime.UtcNow; }
        db.AuditEntries.Add(new AuditEntry { ActorUserId = staffId, EntityType = nameof(CalendarEvent), EntityId = eventId.ToString(), Action = "Support person recorded" });
        await db.SaveChangesAsync(ct);
    }

    public async Task<ClientSupportSession> OpenSupportSessionAsync(int clientId, int staffId, string name, string purpose, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(purpose)) throw new InvalidOperationException("Support person and purpose are required.");
        if (!await db.VulnerableClientFlags.AnyAsync(x => x.ClientId == clientId && x.Safeguard == ClientSafeguard.SupportPerson && Enforced.Contains(x.Status), ct))
            throw new InvalidOperationException("This client does not have an active support-person safeguard.");
        var now = DateTime.UtcNow;
        var session = new ClientSupportSession { ClientId = clientId, AuthorisedByStaffUserId = staffId,
            SupportPersonName = name.Trim(), Purpose = purpose.Trim(), StartsAtUtc = now, ExpiresAtUtc = now.AddHours(Math.Max(1, options.Value.SupportSessionHours)) };
        db.ClientSupportSessions.Add(session);
        db.AuditEntries.Add(new AuditEntry { ActorUserId = staffId, EntityType = nameof(ClientSupportSession), EntityId = clientId.ToString(), Action = "Supported self-service session opened" });
        await db.SaveChangesAsync(ct); return session;
    }

    public async Task<bool> HasActiveSupportSessionAsync(int clientId, CancellationToken ct = default)
    {
        var currentCycle = await db.VulnerableClientFlags.Where(x => x.ClientId == clientId &&
            x.Safeguard == ClientSafeguard.SupportPerson && Enforced.Contains(x.Status))
            .MaxAsync(x => (DateTime?)x.LastChangedAtUtc, ct);
        if (!currentCycle.HasValue) return true;
        var now = DateTime.UtcNow;
        return await db.ClientSupportSessions.AnyAsync(x => x.ClientId == clientId &&
            x.StartsAtUtc >= currentCycle.Value && x.StartsAtUtc <= now &&
            x.ExpiresAtUtc > now && x.RevokedAtUtc == null, ct);
    }

    public async Task<int> RunGovernanceAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow; var changed = 0;
        var directors = await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct);
        foreach (var flag in await db.VulnerableClientFlags.Where(x =>
            (x.Status == VulnerableFlagStatus.PendingReview && x.ReviewDueAtUtc < now) ||
            (x.Status == VulnerableFlagStatus.Confirmed && x.NextReviewAtUtc < now)).ToListAsync(ct))
        {
            if (flag.Status == VulnerableFlagStatus.Confirmed)
            {
                flag.Status = VulnerableFlagStatus.PendingReview; flag.LastChangedAtUtc = now;
                flag.ReviewDueAtUtc = now.AddDays(Math.Max(1, options.Value.DirectorReviewDays)); flag.NextReviewAtUtc = null;
                Audit(null, flag, "Periodic safeguard review became due");
            }
            else flag.Status = VulnerableFlagStatus.Escalated;
            foreach (var director in directors)
                await notifications.QueueAsync(director, "VulnerableClientEscalation", "Client safeguard review overdue",
                    $"Safeguard {flag.Id} for client {flag.ClientId} requires urgent review.", $"/VulnerableClient/Review/{flag.Id}",
                    $"vulnerable-flag-escalated-{flag.Id}-{flag.LastChangedAtUtc:yyyyMMddHH}-{director}", ct);
            changed++;
        }
        await db.SaveChangesAsync(ct); return changed;
    }

    private void Audit(int? actor, VulnerableClientFlag flag, string action) =>
        db.AuditEntries.Add(new AuditEntry { ActorUserId = actor, EntityType = nameof(VulnerableClientFlag),
            EntityId = flag.Id.ToString(), Action = action,
            SafeMetadataJson = JsonSerializer.Serialize(new { flag.ClientId, flag.Safeguard, flag.Status }) });
}

public sealed class VulnerableClientGovernanceWorker(IServiceScopeFactory scopes, ILogger<VulnerableClientGovernanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<IVulnerableClientService>().RunGovernanceAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Vulnerable-client governance sweep failed."); }
            try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
