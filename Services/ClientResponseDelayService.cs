using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;

namespace SimplexLawFirm.Services;

public interface IClientResponseDelayService
{
    Task<CaseDocumentRequest> CreateAsync(CaseDocumentRequest input, int attorneyId, CancellationToken ct = default);
    Task MonitorOverdueAsync(DateTime nowUtc, CancellationToken ct = default);
    Task RecordActionAsync(int requestId, int attorneyId, ClientResponseDelayAction action, string note, DateTime? extendedDeadlineUtc, CancellationToken ct = default);
    Task<IReadOnlyList<ClientResponseSlaRule>> GetRulesAsync(CancellationToken ct = default);
}

public sealed class ClientResponseDelayService(ApplicationDbContext db, INotificationService notifications) : IClientResponseDelayService
{
    public async Task<CaseDocumentRequest> CreateAsync(CaseDocumentRequest input, int attorneyId, CancellationToken ct = default)
    {
        var matter = await db.Cases.Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == input.CaseId && x.LawyerId == attorneyId && x.Status == CaseStatus.Active, ct)
            ?? throw new UnauthorizedAccessException("Only the assigned attorney may request a response for an active matter.");
        if (string.IsNullOrWhiteSpace(matter.Client.Email) && string.IsNullOrWhiteSpace(matter.Client.Phone)) throw new InvalidOperationException("The client must have a valid email address or telephone number before a response can be requested.");
        if (string.IsNullOrWhiteSpace(input.Title) || (input.RequestType == ClientResponseRequestType.Document && string.IsNullOrWhiteSpace(input.RequiredDocuments))) throw new InvalidOperationException("Provide a title and the required documents for a document request.");
        var rule = await RuleForAsync(input.RequestType, ct);
        input.ClientId = matter.ClientId; input.RequestedByAttorneyId = attorneyId; input.Title = input.Title.Trim();
        input.RequiredDocuments = input.RequiredDocuments?.Trim() ?? ""; input.Instructions = input.Instructions?.Trim() ?? "";
        input.CreatedAtUtc = DateTime.UtcNow; input.DueAtUtc = input.CreatedAtUtc.AddDays(rule.ResponseDays); input.ResponseSlaDaysSnapshot = rule.ResponseDays; input.Status = CaseDocumentRequestStatus.Requested;
        db.CaseDocumentRequests.Add(input);
        await db.SaveChangesAsync(ct);
        db.AuditEntries.Add(new AuditEntry { ActorUserId = attorneyId, EntityType = nameof(CaseDocumentRequest), EntityId = input.Id.ToString(), Action = "Client response requested", SafeMetadataJson = JsonSerializer.Serialize(new { input.RequestType, input.DueAtUtc, input.ResponseSlaDaysSnapshot }) });
        var clientUser = await db.Users.SingleOrDefaultAsync(x => x.Email == matter.Client.Email, ct);
        if (clientUser is not null) await notifications.QueueAsync(clientUser.Id, "ClientResponseRequest", $"Response requested for {matter.CaseNumber}", $"{input.Title}. Please respond by {input.DueAtUtc:dd MMM yyyy}.", $"/CaseDocumentRequest/Respond/{input.Id}", $"client-response-request-{input.Id}", ct);
        await db.SaveChangesAsync(ct);
        return input;
    }

    public async Task MonitorOverdueAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var requests = await db.CaseDocumentRequests.Include(x => x.Case).Include(x => x.Client)
            .Where(x => (x.Status == CaseDocumentRequestStatus.Requested || x.Status == CaseDocumentRequestStatus.MoreInformationRequired) && x.DueAtUtc <= nowUtc).ToListAsync(ct);
        foreach (var request in requests)
        {
            request.Status = CaseDocumentRequestStatus.Overdue; request.OverdueAtUtc = nowUtc;
            db.AuditEntries.Add(new AuditEntry { EntityType = nameof(CaseDocumentRequest), EntityId = request.Id.ToString(), Action = "Client response overdue", SafeMetadataJson = JsonSerializer.Serialize(new { request.CaseId, request.DueAtUtc, request.RequestType }) });
            await notifications.QueueAsync(request.RequestedByAttorneyId, "ClientResponseOverdue", $"Client response overdue: {request.Case.CaseNumber}", $"{request.Client.FullName} has not responded to '{request.Title}', due {request.DueAtUtc:dd MMM yyyy}.", $"/CaseDocumentRequest/Details/{request.Id}", $"client-response-overdue-{request.Id}", ct);
        }
        if (requests.Count > 0) await db.SaveChangesAsync(ct);
    }

    public async Task RecordActionAsync(int requestId, int attorneyId, ClientResponseDelayAction action, string note, DateTime? extendedDeadlineUtc, CancellationToken ct = default)
    {
        var request = await db.CaseDocumentRequests.SingleOrDefaultAsync(x => x.Id == requestId && x.RequestedByAttorneyId == attorneyId, ct) ?? throw new UnauthorizedAccessException();
        if (request.Status != CaseDocumentRequestStatus.Overdue) throw new InvalidOperationException("Only an overdue response can be actioned through this workflow.");
        if (string.IsNullOrWhiteSpace(note)) throw new InvalidOperationException("Record the action taken and its reason.");
        if (action == ClientResponseDelayAction.DeadlineExtended)
        {
            if (extendedDeadlineUtc is null || extendedDeadlineUtc <= DateTime.UtcNow) throw new InvalidOperationException("A future extended deadline is required.");
            request.DueAtUtc = extendedDeadlineUtc.Value; request.Status = CaseDocumentRequestStatus.Requested; request.OverdueAtUtc = null;
        }
        request.LastDelayAction = action; request.LastDelayActionAtUtc = DateTime.UtcNow; request.LastDelayActionNote = note.Trim();
        db.AuditEntries.Add(new AuditEntry { ActorUserId = attorneyId, EntityType = nameof(CaseDocumentRequest), EntityId = request.Id.ToString(), Action = $"Overdue response action: {action}", SafeMetadataJson = JsonSerializer.Serialize(new { request.CaseId, note = request.LastDelayActionNote, extendedDeadlineUtc }) });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ClientResponseSlaRule>> GetRulesAsync(CancellationToken ct = default)
    {
        foreach (var (type, days) in Defaults)
            if (!await db.ClientResponseSlaRules.AnyAsync(x => x.RequestType == type, ct)) db.ClientResponseSlaRules.Add(new ClientResponseSlaRule { RequestType = type, ResponseDays = days });
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct);
        return await db.ClientResponseSlaRules.OrderBy(x => x.RequestType).ToListAsync(ct);
    }

    private async Task<ClientResponseSlaRule> RuleForAsync(ClientResponseRequestType type, CancellationToken ct) => (await GetRulesAsync(ct)).Single(x => x.RequestType == type && x.IsActive);
    private static readonly (ClientResponseRequestType Type, int Days)[] Defaults = [(ClientResponseRequestType.Information, 5), (ClientResponseRequestType.Document, 7), (ClientResponseRequestType.Approval, 3), (ClientResponseRequestType.Decision, 5)];
}

public sealed class ClientResponseDelayWorker(IServiceScopeFactory scopes, ILogger<ClientResponseDelayWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<IClientResponseDelayService>().MonitorOverdueAsync(DateTime.UtcNow, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Client response delay cycle failed."); }
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
