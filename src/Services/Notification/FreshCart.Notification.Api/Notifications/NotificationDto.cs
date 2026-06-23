using System.Globalization;

namespace FreshCart.Notification.Api.Notifications;

/// <summary>
/// Wire shape sent to the SPA over both the REST history endpoints and the SignalR
/// <c>notificationReceived</c> push. Identifiers and timestamps are serialised as strings so the
/// browser receives the exact contract the gateway documents.
/// </summary>
public sealed record NotificationDto(
    string Id,
    string Type,
    string Title,
    string Message,
    string? OrderId,
    string CreatedOnUtc,
    bool IsRead)
{
    public static NotificationDto FromDocument(NotificationDocument notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return new NotificationDto(
            notification.Id.ToString(),
            notification.Type,
            notification.Title,
            notification.Message,
            notification.OrderId?.ToString(),
            notification.CreatedOnUtc.ToString("O", CultureInfo.InvariantCulture),
            notification.IsRead);
    }
}
