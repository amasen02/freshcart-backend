using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Notifications;
using MassTransit;

namespace FreshCart.Notification.Api.Consumers;

/// <summary>
/// Notifies the customer that their order was cancelled, carrying the reason supplied by Ordering.
/// </summary>
public sealed class OrderCancelledConsumer(NotificationRecorder notificationRecorder)
    : IConsumer<OrderCancelledIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderCancelledIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var integrationEvent = context.Message;
        var content = NotificationContentFactory.ForOrderCancelled(integrationEvent);

        return notificationRecorder.RecordAndDispatchAsync(
            integrationEvent.CustomerId,
            integrationEvent.EventId,
            content,
            integrationEvent.OrderId,
            context.CancellationToken);
    }
}
