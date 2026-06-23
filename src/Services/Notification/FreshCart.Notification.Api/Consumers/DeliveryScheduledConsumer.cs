using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Notifications;
using MassTransit;

namespace FreshCart.Notification.Api.Consumers;

/// <summary>
/// Notifies the customer of the booked delivery slot.
/// </summary>
public sealed class DeliveryScheduledConsumer(NotificationRecorder notificationRecorder)
    : IConsumer<DeliveryScheduledIntegrationEvent>
{
    public Task Consume(ConsumeContext<DeliveryScheduledIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var integrationEvent = context.Message;
        var content = NotificationContentFactory.ForDeliveryScheduled(integrationEvent);

        return notificationRecorder.RecordAndDispatchAsync(
            integrationEvent.CustomerId,
            integrationEvent.EventId,
            content,
            integrationEvent.OrderId,
            context.CancellationToken);
    }
}
