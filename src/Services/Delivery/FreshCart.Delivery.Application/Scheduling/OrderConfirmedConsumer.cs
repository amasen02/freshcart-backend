using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Delivery.Application.Scheduling;

/// <summary>
/// Schedules a delivery when an order is confirmed. Idempotency (one delivery per order) lives in
/// <see cref="ScheduleDeliveryService"/>, so a redelivered message resolves to an
/// <see cref="ScheduleDeliveryResult.AlreadyScheduled"/> outcome and publishes nothing a second time.
/// A <see cref="Shipments.PendingShipmentNotYetAvailableException"/> is intentionally left to escape so
/// the MassTransit retry policy redelivers when the checkout event lost the race to the confirmation.
/// </summary>
public sealed partial class OrderConfirmedConsumer(
    ScheduleDeliveryService scheduleDeliveryService,
    IPublishEndpoint publishEndpoint,
    ILogger<OrderConfirmedConsumer> logger)
    : IConsumer<OrderConfirmedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderConfirmedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var orderId = context.Message.OrderId;
        var outcome = await scheduleDeliveryService
            .ScheduleForConfirmedOrderAsync(orderId, context.CancellationToken)
            .ConfigureAwait(false);

        if (outcome.Result != ScheduleDeliveryResult.Scheduled)
        {
            LogNoEventPublished(orderId, outcome.Result);
            return;
        }

        var delivery = outcome.Delivery!;
        await publishEndpoint
            .Publish(
                new DeliveryScheduledIntegrationEvent
                {
                    OrderId = delivery.OrderId,
                    DeliveryId = delivery.Id,
                    CustomerId = delivery.CustomerId,
                    SlotStartUtc = delivery.SlotStartUtc,
                    SlotEndUtc = delivery.SlotEndUtc,
                },
                context.CancellationToken)
            .ConfigureAwait(false);

        LogDeliveryScheduledEventPublished(orderId, delivery.Id);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Order {OrderId} produced scheduling outcome {Outcome}; no DeliveryScheduled event published")]
    private partial void LogNoEventPublished(Guid orderId, ScheduleDeliveryResult outcome);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Published DeliveryScheduled for order {OrderId} delivery {DeliveryId}")]
    private partial void LogDeliveryScheduledEventPublished(Guid orderId, Guid deliveryId);
}
