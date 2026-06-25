using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Delivery.Application.Scheduling;

/// <summary>
/// Schedules a delivery when an order is confirmed. The DeliveryScheduled event is not published here;
/// <see cref="ScheduleDeliveryService"/> stages it in the transactional outbox in the same transaction
/// that writes the delivery, and the background publisher delivers it, so a broker failure cannot drop
/// it. Idempotency (one delivery per order) lives in the service, so a redelivered message resolves to an
/// <see cref="ScheduleDeliveryResult.AlreadyScheduled"/> outcome and stages nothing a second time. A
/// <see cref="Shipments.PendingShipmentNotYetAvailableException"/> (and a slot-lost race) is intentionally
/// left to escape so the MassTransit retry policy redelivers.
/// </summary>
public sealed partial class OrderConfirmedConsumer(
    ScheduleDeliveryService scheduleDeliveryService,
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

        LogSchedulingOutcome(orderId, outcome.Result);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Order {OrderId} produced scheduling outcome {Outcome}")]
    private partial void LogSchedulingOutcome(Guid orderId, ScheduleDeliveryResult outcome);
}
