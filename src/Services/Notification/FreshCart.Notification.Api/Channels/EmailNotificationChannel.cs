using FreshCart.Notification.Api.Notifications;

namespace FreshCart.Notification.Api.Channels;

/// <summary>
/// Renders a notification to a plain-text mail and hands it to the configured <see cref="IEmailSender"/>.
/// </summary>
public sealed class EmailNotificationChannel(IEmailSender emailSender) : INotificationChannel
{
    public Task SendAsync(NotificationDocument notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var message = PlainTextEmailRenderer.Render(notification);
        return emailSender.SendAsync(message, cancellationToken);
    }
}
