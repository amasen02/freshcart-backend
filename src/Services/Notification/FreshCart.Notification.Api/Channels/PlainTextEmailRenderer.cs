using FreshCart.Notification.Api.Notifications;

namespace FreshCart.Notification.Api.Channels;

/// <summary>
/// Renders a notification into a plain-text mail. Pure and side-effect free so the wording can be
/// asserted directly in tests without a transport.
/// </summary>
public static class PlainTextEmailRenderer
{
    private const string SignatureLine = "The FreshCart team";

    public static EmailMessage Render(NotificationDocument notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var bodyBuilder = new System.Text.StringBuilder();
        bodyBuilder.Append(notification.Message);

        if (notification.OrderId is { } orderId)
        {
            bodyBuilder.Append("\n\nOrder reference: ").Append(orderId);
        }

        bodyBuilder.Append("\n\n").Append(SignatureLine);

        return new EmailMessage(notification.Title, bodyBuilder.ToString());
    }
}
