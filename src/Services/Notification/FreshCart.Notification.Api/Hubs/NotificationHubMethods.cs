namespace FreshCart.Notification.Api.Hubs;

/// <summary>
/// Server-to-client method names the SPA subscribes to. Centralised so the hub and the channel that
/// pushes through it cannot drift apart.
/// </summary>
public static class NotificationHubMethods
{
    public const string NotificationReceived = "notificationReceived";

    public const string SalesDashboardUpdated = "salesDashboardUpdated";
}
