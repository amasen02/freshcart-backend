using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Notifications;
using MassTransit;

namespace FreshCart.Notification.Api.Consumers;

/// <summary>
/// Notifies the customer that their order has been delivered.
/// </summary>
public sealed class DeliveryCompletedConsumer(NotificationRecorder notificationRecorder)
    : IConsumer<DeliveryCompletedIntegrationEvent>
{
    public Task Consume(ConsumeContext<DeliveryCompletedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var integrationEvent = context.Message;
        var content = NotificationContentFactory.ForDeliveryCompleted(integrationEvent);

        return notificationRecorder.RecordAndDispatchAsync(
            integrationEvent.CustomerId,
            integrationEvent.EventId,
            content,
            integrationEvent.OrderId,
            context.CancellationToken);
    }
}
