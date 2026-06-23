using FreshCart.Notification.Api.Notifications;

namespace FreshCart.Notification.Api.Channels;

/// <summary>
/// A single fan-out destination for a stored notification (real-time push, email, and so on).
/// Implementations must be self-contained: the dispatcher isolates failures per channel, so a
/// channel that throws only takes itself down.
/// </summary>
public interface INotificationChannel
{
    Task SendAsync(NotificationDocument notification, CancellationToken cancellationToken);
}
