using FreshCart.Payment.Infrastructure.EventStore;
using FreshCart.Payment.Infrastructure.Projections;
using FreshCart.Payment.Infrastructure.ReadModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreshCart.Payment.Infrastructure;

/// <summary>
/// Applies the idempotent read-model schema, the event-store indexes and the projection-outbox poll
/// index on startup so a fresh environment is queryable and projectable before the first request arrives.
/// </summary>
public sealed partial class PaymentPersistenceInitializer(
    IServiceScopeFactory serviceScopeFactory,
    MongoPaymentEventStore paymentEventStore,
    MongoPaymentProjectionOutbox projectionOutbox,
    ILogger<PaymentPersistenceInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using (serviceScope.ConfigureAwait(false))
        {
            var connectionFactory = serviceScope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
            var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await PaymentReadModelSchema.EnsureCreatedAsync(connection, cancellationToken).ConfigureAwait(false);
        }

        await paymentEventStore.EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);
        await projectionOutbox.EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

        LogPersistenceVerified();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Payment persistence verified")]
    private partial void LogPersistenceVerified();
}
