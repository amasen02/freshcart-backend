using FreshCart.Notification.Api.Channels;
using Microsoft.Extensions.Logging;

namespace FreshCart.Notification.Api.Notifications;

/// <summary>
/// Shared store-then-fan-out path every consumer runs. The notification is persisted first; only a
/// genuine first store triggers the dispatcher, so a redelivered event the unique index rejects is
/// acknowledged without re-notifying. Bus redelivery therefore covers store failures alone.
/// </summary>
public sealed partial class NotificationRecorder(
    INotificationStore notificationStore,
    NotificationDispatcher notificationDispatcher,
    TimeProvider timeProvider,
    ILogger<NotificationRecorder> logger)
{
    /// <summary>
    /// Persists then fans out a notification. Returns the store outcome so a consumer that has a
    /// further first-delivery-only side effect (the live sales tick) can run it only when this was a
    /// genuine first store.
    /// </summary>
    public async Task<AddNotificationOutcome> RecordAndDispatchAsync(
        Guid recipientUserId,
        Guid sourceEventId,
        NotificationContent content,
        Guid? orderId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var notification = new NotificationDocument
        {
            Id = Guid.CreateVersion7(),
            UserId = recipientUserId,
            SourceEventId = sourceEventId,
            Type = content.Type,
            Title = content.Title,
            Message = content.Message,
            OrderId = orderId,
            CreatedOnUtc = timeProvider.GetUtcNow(),
            IsRead = false,
        };

        var outcome = await notificationStore.AddAsync(notification, cancellationToken).ConfigureAwait(false);

        if (outcome == AddNotificationOutcome.DuplicateIgnored)
        {
            LogDuplicateIgnored(notification.SourceEventId, notification.UserId);
            return outcome;
        }

        await notificationDispatcher.DispatchAsync(notification, cancellationToken).ConfigureAwait(false);
        return outcome;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Ignored duplicate notification for event {SourceEventId} and user {UserId}")]
    private partial void LogDuplicateIgnored(Guid sourceEventId, Guid userId);
}
