using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Notifications;
using MassTransit;

namespace FreshCart.Notification.Api.Consumers;

/// <summary>
/// Notifies the customer that their order was received. Idempotency rides on the
/// (UserId, SourceEventId) unique index the store enforces, so a redelivery is acknowledged without a
/// second notification.
/// </summary>
public sealed class OrderPlacedConsumer(NotificationRecorder notificationRecorder)
    : IConsumer<OrderPlacedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderPlacedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var integrationEvent = context.Message;
        var content = NotificationContentFactory.ForOrderPlaced(integrationEvent);

        return notificationRecorder.RecordAndDispatchAsync(
            integrationEvent.CustomerId,
            integrationEvent.EventId,
            content,
            integrationEvent.OrderId,
            context.CancellationToken);
    }
}
