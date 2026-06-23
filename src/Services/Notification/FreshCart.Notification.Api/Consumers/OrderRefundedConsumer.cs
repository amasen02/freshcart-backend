using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Notifications;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Notification.Api.Consumers;

/// <summary>
/// Notifies the customer that a refund was issued. The OrderRefunded contract carries only the order
/// id, so the recipient is recovered from the OrderConfirmed entry the saga stored earlier; if none
/// is found yet the event is acknowledged (there is no addressable recipient to notify).
/// </summary>
public sealed partial class OrderRefundedConsumer(
    INotificationStore notificationStore,
    NotificationRecorder notificationRecorder,
    ILogger<OrderRefundedConsumer> logger)
    : IConsumer<OrderRefundedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderRefundedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var integrationEvent = context.Message;

        var recipientUserId = await notificationStore
            .FindRecipientByOrderAsync(integrationEvent.OrderId, context.CancellationToken)
            .ConfigureAwait(false);

        if (recipientUserId is not { } userId)
        {
            LogRecipientUnknown(integrationEvent.OrderId);
            throw new InvalidOperationException(
                $"No notification recipient is known yet for order {integrationEvent.OrderId}; " +
                "throwing so the broker redelivers once the OrderConfirmed projection has caught up.");
        }

        var content = NotificationContentFactory.ForOrderRefunded(integrationEvent);

        await notificationRecorder
            .RecordAndDispatchAsync(
                userId,
                integrationEvent.EventId,
                content,
                integrationEvent.OrderId,
                context.CancellationToken)
            .ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "No notification recipient is known yet for order {OrderId}; OrderRefunded will be retried, then dead-lettered if the retry budget is exhausted")]
    private partial void LogRecipientUnknown(Guid orderId);
}
