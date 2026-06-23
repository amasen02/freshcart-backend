using FreshCart.Notification.Api.Notifications;
using Microsoft.Extensions.Logging;

namespace FreshCart.Notification.Api.Channels;

/// <summary>
/// Fans a stored notification out across every registered channel. A failing channel is logged and
/// swallowed so the remaining channels still run: the durable record already exists in the store, and
/// bus redelivery is reserved for store failures alone (a fan-out retry would re-notify the healthy
/// channels). This is the whole point of the service, hence its only collaborator is the channel set.
/// </summary>
public sealed partial class NotificationDispatcher(
    IEnumerable<INotificationChannel> channels,
    ILogger<NotificationDispatcher> logger)
{
    private readonly IReadOnlyList<INotificationChannel> channels = channels.ToArray();

    public async Task DispatchAsync(NotificationDocument notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        foreach (var channel in channels)
        {
            try
            {
                await channel.SendAsync(notification, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception channelFailure)
            {
                LogChannelFailed(channelFailure, channel.GetType().Name, notification.Id);
            }
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Notification channel {ChannelType} failed to deliver notification {NotificationId}; other channels continue")]
    private partial void LogChannelFailed(Exception exception, string channelType, Guid notificationId);
}
