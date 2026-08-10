namespace SimplexLawFirm.Services.Billing;
public sealed class AppointmentBillingWorker(IServiceScopeFactory scopes, ILogger<AppointmentBillingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { using var scope = scopes.CreateScope(); var billing = scope.ServiceProvider.GetRequiredService<IAppointmentBillingService>(); var now = DateTime.UtcNow; await billing.ProcessDueAsync(now, ct); await billing.ApplyPenaltiesAsync(now, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Appointment billing cycle failed."); }
            try { await Task.Delay(TimeSpan.FromMinutes(5), ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }
}
