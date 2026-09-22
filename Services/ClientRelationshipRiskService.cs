using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Services;

/// <summary>Transparent, scheduled relationship-risk analysis. This is a rules-based
/// decision-support score, not an AI model and must not be used as an automated decision.</summary>
public sealed class ClientRelationshipRiskOptions
{
    public int HighRiskThreshold { get; set; } = 70;
    public int OverdueInvoiceWeight { get; set; } = 25;
    public int OpenComplaintWeight { get; set; } = 20;
    public int OverdueResponseWeight { get; set; } = 15;
    public int UnansweredCorrespondenceWeight { get; set; } = 10;
}

public interface IClientRelationshipRiskService
{
    Task RecalculateAsync(CancellationToken ct = default);
}

public sealed class ClientRelationshipRiskService(ApplicationDbContext db, IOptions<ClientRelationshipRiskOptions> options) : IClientRelationshipRiskService
{
    public async Task RecalculateAsync(CancellationToken ct = default)
    {
        var policy = options.Value;
        var clients = await db.Clients.Where(x => x.IsActive).ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var client in clients)
        {
            var overdueInvoices = await db.Invoices.CountAsync(x => x.ClientId == client.Id && !x.IsPaid && x.DueDate < now, ct);
            var latePayments = await db.Invoices.CountAsync(x => x.ClientId == client.Id && x.IsPaid && x.PaidDate > x.DueDate, ct);
            var openComplaints = await db.ServiceComplaints.CountAsync(x => x.ClientId == client.Id && x.Status != ComplaintStatus.Resolved, ct);
            var overdueResponses = await db.CaseDocumentRequests.CountAsync(x => x.ClientId == client.Id && x.Status == CaseDocumentRequestStatus.Overdue, ct);
            var unanswered = await db.ClientCorrespondence.CountAsync(x => x.Case.ClientId == client.Id && x.AnsweredAtUtc == null && x.ReceivedAtUtc < now.AddDays(-3), ct);

            // Financial: payment trends. Satisfaction: open complaints. Communication:
            // overdue client responses and unanswered correspondence. Each component is
            // capped and the weights/alert threshold are deployment configuration.
            var financial = Math.Min(100m, overdueInvoices * policy.OverdueInvoiceWeight + latePayments * 10m);
            var satisfaction = Math.Min(100m, openComplaints * policy.OpenComplaintWeight);
            var communication = Math.Min(100m, overdueResponses * policy.OverdueResponseWeight + unanswered * policy.UnansweredCorrespondenceWeight);
            var reputation = Math.Min(100m, openComplaints * 15m + unanswered * 5m);
            var overall = decimal.Round((financial * .35m) + (satisfaction * .30m) + (communication * .25m) + (reputation * .10m), 2);
            var risk = await db.ClientRelationshipRisks.SingleOrDefaultAsync(x => x.ClientId == client.Id, ct);
            if (risk is null) { risk = new ClientRelationshipRisk { ClientId = client.Id }; db.ClientRelationshipRisks.Add(risk); }
            risk.FinancialScore = financial;
            risk.LegalScore = satisfaction;
            risk.ReputationScore = reputation;
            risk.PredictedRelationshipScore = overall;
            risk.OverallScore = overall;
            risk.HighRiskAlert = overall >= policy.HighRiskThreshold;
            risk.Explanation = $"Payment trend: {overdueInvoices} overdue invoice(s), {latePayments} late payment(s); satisfaction signal: {openComplaints} open complaint(s); communication: {overdueResponses} overdue response(s), {unanswered} unanswered message(s) older than 3 days. Configured alert threshold: {policy.HighRiskThreshold}.";
            risk.CalculatedAtUtc = now;
        }
        await db.SaveChangesAsync(ct);
    }
}

public sealed class ClientRelationshipRiskWorker(IServiceScopeFactory scopes, ILogger<ClientRelationshipRiskWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<IClientRelationshipRiskService>().RecalculateAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Scheduled client relationship-risk analysis failed."); }
            try { await Task.Delay(TimeSpan.FromDays(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
