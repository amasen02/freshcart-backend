using FreshCart.Notification.Api.Channels;
using FreshCart.Notification.Api.Notifications;

namespace FreshCart.Notification.Tests.Fakes;

/// <summary>Channel that records every notification it is asked to deliver.</summary>
public sealed class RecordingNotificationChannel : INotificationChannel
{
    private readonly List<NotificationDocument> delivered = [];

    public IReadOnlyList<NotificationDocument> Delivered => delivered;

    public Task SendAsync(NotificationDocument notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        delivered.Add(notification);
        return Task.CompletedTask;
    }
}
