using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Inventory.Api.Services;
using MassTransit;

namespace FreshCart.Inventory.Api.Consumers;

/// <summary>
/// Safety net for cancellations that bypass the ordering saga's gRPC release call; releasing is
/// idempotent, so a duplicate of the saga path is harmless.
/// </summary>
public sealed partial class OrderCancelledConsumer(
    IStockReservationService stockReservationService,
    ILogger<OrderCancelledConsumer> logger) : IConsumer<OrderCancelledIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var orderCancelled = context.Message;

        var released = await stockReservationService
            .ReleaseAsync(orderCancelled.OrderId, context.CancellationToken)
            .ConfigureAwait(false);

        if (released)
        {
            LogReservationReleasedForCancelledOrder(orderCancelled.OrderId);
        }
        else
        {
            LogNoActiveReservationToRelease(orderCancelled.OrderId);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Released stock reservation for cancelled order {OrderId}")]
    private partial void LogReservationReleasedForCancelledOrder(Guid orderId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "No active stock reservation to release for cancelled order {OrderId}")]
    private partial void LogNoActiveReservationToRelease(Guid orderId);
}
