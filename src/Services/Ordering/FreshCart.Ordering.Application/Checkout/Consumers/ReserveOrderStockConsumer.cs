using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Checkout.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Ordering.Application.Checkout.Consumers;

/// <summary>
/// Calls the Inventory service to reserve stock for the order, then publishes the outcome as the
/// integration event the saga correlates on. A reservation rejection is a business result and comes
/// back as a published failure event; a transport fault throws so the bus retry policy re-runs the
/// reservation. Inventory keys reservations by order id, so retries are idempotent on its side.
/// </summary>
public sealed partial class ReserveOrderStockConsumer(
    IInventoryClient inventoryClient,
    ILogger<ReserveOrderStockConsumer> logger) : IConsumer<ReserveOrderStock>
{
    public async Task Consume(ConsumeContext<ReserveOrderStock> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var orderId = context.Message.OrderId;

        var reservationLines = context.Message.Lines
            .Select(line => new StockReservationLine(line.ProductSku, line.Quantity))
            .ToList();

        var reservation = await inventoryClient
            .ReserveStockAsync(orderId, reservationLines, context.CancellationToken)
            .ConfigureAwait(false);

        if (reservation.Succeeded && reservation.ReservationId.HasValue)
        {
            LogStockReserved(orderId, reservation.ReservationId.Value);

            await context.Publish(new StockReservedIntegrationEvent
            {
                OrderId = orderId,
                ReservationId = reservation.ReservationId.Value,
            }).ConfigureAwait(false);

            return;
        }

        var reason = reservation.FailureReason ?? "Inventory could not reserve the requested stock.";
        LogStockReservationFailed(orderId, reason);

        await context.Publish(new StockReservationFailedIntegrationEvent
        {
            OrderId = orderId,
            Reason = reason,
            UnavailableSkus = reservation.UnavailableSkus,
        }).ConfigureAwait(false);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Reserved stock for order {OrderId} under reservation {ReservationId}")]
    private partial void LogStockReserved(Guid orderId, Guid reservationId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Stock reservation failed for order {OrderId}: {Reason}")]
    private partial void LogStockReservationFailed(Guid orderId, string reason);
}
