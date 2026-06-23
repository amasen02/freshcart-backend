using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Notifications;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Notification.Api.Consumers;

/// <summary>
/// Notifies the customer that their payment failed. The PaymentFailed contract carries only the order
/// id, so the recipient is recovered from the OrderPlaced entry the saga stored earlier; if none is
/// found yet the event is acknowledged (there is no addressable recipient to notify).
/// </summary>
public sealed partial class PaymentFailedConsumer(
    INotificationStore notificationStore,
    NotificationRecorder notificationRecorder,
    ILogger<PaymentFailedConsumer> logger)
    : IConsumer<PaymentFailedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedIntegrationEvent> context)
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
                "throwing so the broker redelivers once the OrderPlaced projection has caught up.");
        }

        var content = NotificationContentFactory.ForPaymentFailed(integrationEvent);

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
        Message = "No notification recipient is known yet for order {OrderId}; PaymentFailed will be retried, then dead-lettered if the retry budget is exhausted")]
    private partial void LogRecipientUnknown(Guid orderId);
}
