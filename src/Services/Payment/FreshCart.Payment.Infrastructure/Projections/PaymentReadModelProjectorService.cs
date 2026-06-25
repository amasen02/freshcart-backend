using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreshCart.Payment.Infrastructure.Projections;

/// <summary>
/// Hosts the <see cref="PaymentReadModelProjector"/> as a polling background worker. A fresh DI scope is
/// created per cycle so the scoped SQL read-model writer (and its connection) is not held for the host's
/// lifetime. A cycle failure is logged and retried on the next poll rather than tearing the host down.
/// </summary>
public sealed partial class PaymentReadModelProjectorService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<PaymentProjectionOptions> projectionOptions,
    ILogger<PaymentReadModelProjectorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = projectionOptions.Value;
        LogProjectorStarting(options.BatchSize, options.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var serviceScope = serviceScopeFactory.CreateScope();
                var projector = serviceScope.ServiceProvider.GetRequiredService<PaymentReadModelProjector>();
                await projector.ProjectPendingAsync(options.BatchSize, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // expected on shutdown
            }
            catch (Exception cycleFailure)
            {
                LogProjectionCycleFailed(cycleFailure);
            }

            try
            {
                await Task.Delay(options.PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Payment read-model projector starting with batch size {BatchSize} and poll interval {PollInterval}")]
    private partial void LogProjectorStarting(int batchSize, TimeSpan pollInterval);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Payment projection cycle failed; will retry after the poll interval")]
    private partial void LogProjectionCycleFailed(Exception exception);
}
