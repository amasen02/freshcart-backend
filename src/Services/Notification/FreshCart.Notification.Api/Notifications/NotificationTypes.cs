namespace FreshCart.Notification.Api.Notifications;

/// <summary>
/// Stable string discriminators stored on every <see cref="NotificationDocument"/> and surfaced to the
/// SPA. They are persisted, so the values are part of the read contract and must not be renamed.
/// </summary>
public static class NotificationTypes
{
    public const string OrderPlaced = "OrderPlaced";
    public const string OrderConfirmed = "OrderConfirmed";
    public const string PaymentFailed = "PaymentFailed";
    public const string OrderCancelled = "OrderCancelled";
    public const string OrderRefunded = "OrderRefunded";
    public const string DeliveryScheduled = "DeliveryScheduled";
    public const string DeliveryCompleted = "DeliveryCompleted";
}
