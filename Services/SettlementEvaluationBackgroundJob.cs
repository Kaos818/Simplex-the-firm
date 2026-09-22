using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimplexLawFirm.Services
{
    /// <summary>
    /// Optional hosted service that runs the settlement evaluation periodically.
    /// UC-06 Step 1 (scheduled path).
    ///
    /// File: Services/SettlementEvaluationBackgroundJob.cs
    ///
    /// Alternatively, call SettlementService.RunPeriodicEvaluationAsync()
    /// from Hangfire: RecurringJob.AddOrUpdate<ISettlementService>(
    ///     "settlement-evaluation",
    ///     s => s.RunPeriodicEvaluationAsync(),
    ///     Cron.Weekly);
    /// </summary>
    public class SettlementEvaluationBackgroundJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SettlementEvaluationBackgroundJob> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromDays(7); // weekly

        public SettlementEvaluationBackgroundJob(
            IServiceScopeFactory scopeFactory,
            ILogger<SettlementEvaluationBackgroundJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ISettlementService>();
                    var results = await service.RunPeriodicEvaluationAsync();

                    _logger.LogInformation(
                        "Settlement evaluation complete. Evaluated: {Count}, Recommended: {Recommended}",
                        results.Count, results.Count(r => r.SettlementRecommended));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Settlement periodic evaluation failed.");
                }

                try { await Task.Delay(_interval, stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }
    }
}
