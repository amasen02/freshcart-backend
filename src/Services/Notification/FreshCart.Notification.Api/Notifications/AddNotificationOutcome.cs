namespace FreshCart.Notification.Api.Notifications;

/// <summary>
/// Result of an attempt to persist a notification. <see cref="Stored"/> distinguishes a first
/// delivery (write happened, fan-out should follow) from a duplicate redelivery the unique index
/// rejected (already delivered once, fan-out must be skipped to avoid double-notifying).
/// </summary>
public enum AddNotificationOutcome
{
    Stored = 0,
    DuplicateIgnored = 1,
}
