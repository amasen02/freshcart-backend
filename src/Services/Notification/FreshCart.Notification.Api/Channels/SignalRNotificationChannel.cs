using FreshCart.Notification.Api.Hubs;
using FreshCart.Notification.Api.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace FreshCart.Notification.Api.Channels;

/// <summary>
/// Pushes a notification to the recipient's live connections through the SignalR hub. With the Redis
/// backplane configured, addressing the <c>user:{userId}</c> group reaches every replica the user is
/// connected to.
/// </summary>
public sealed class SignalRNotificationChannel(IHubContext<NotificationHub> hubContext) : INotificationChannel
{
    public Task SendAsync(NotificationDocument notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return hubContext.Clients
            .Group(NotificationGroups.ForUser(notification.UserId))
            .SendAsync(
                NotificationHubMethods.NotificationReceived,
                NotificationDto.FromDocument(notification),
                cancellationToken);
    }
}
