using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreshCart.Notification.Api.Notifications.Mongo;

/// <summary>
/// Creates the notification collection indexes on startup so the idempotency guarantee and the
/// timeline query are in force before the first event is consumed or the first history request
/// arrives.
/// </summary>
public sealed partial class NotificationIndexInitializer(
    MongoNotificationStore notificationStore,
    ILogger<NotificationIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await notificationStore.EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);
        LogIndexesEnsured();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Notification indexes ensured")]
    private partial void LogIndexesEnsured();
}
