namespace SimplexLawFirm.Services.Billing;
public interface IAppointmentBillingService
{
    Task<int> ProcessDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<int> ApplyPenaltiesAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
    Task ApplyPenaltyAsync(int invoiceId, int accountantUserId, string reason, DateTime nowUtc, CancellationToken cancellationToken = default);
}
