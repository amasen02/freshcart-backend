namespace FreshCart.Notification.Api.Notifications;

/// <summary>
/// The human-facing wording of a notification, decoupled from the integration event that triggered
/// it. Built by <see cref="NotificationContentFactory"/>.
/// </summary>
public sealed record NotificationContent(string Type, string Title, string Message);
