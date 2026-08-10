namespace SimplexLawFirm.Services.Billing;
public sealed class LatePenaltyWorker(IServiceScopeFactory scopes, ILogger<LatePenaltyWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<IAppointmentBillingService>().ApplyPenaltiesAsync(DateTime.UtcNow, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Late penalty cycle failed."); }
            try { await Task.Delay(TimeSpan.FromHours(1), ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }
}
