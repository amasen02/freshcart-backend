using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Domain.Deliveries;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Delivery.Application.Shipments;

/// <summary>
/// Builds the local <see cref="PendingShipment"/> projection from the checkout event, which is the only
/// event in the flow that carries the shipping address and the physical/digital composition of the
/// basket. The upsert is idempotent so a redelivered message overwrites rather than duplicates; the row
/// lives until <see cref="Scheduling.ScheduleDeliveryService"/> consumes it on order confirmation.
/// </summary>
public sealed partial class BasketCheckoutStartedConsumer(
    IPendingShipmentRepository pendingShipmentRepository,
    ILogger<BasketCheckoutStartedConsumer> logger)
    : IConsumer<BasketCheckoutStartedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<BasketCheckoutStartedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        var hasPhysicalLines = message.Lines.Any(line => !line.IsDigital);
        var shippingAddress = ToDeliveryAddress(message.ShippingAddress);

        var pendingShipment = new PendingShipment(
            message.OrderId,
            message.CustomerId,
            shippingAddress,
            hasPhysicalLines);

        await pendingShipmentRepository
            .UpsertAsync(pendingShipment, context.CancellationToken)
            .ConfigureAwait(false);

        LogPendingShipmentRecorded(message.OrderId, hasPhysicalLines, shippingAddress is not null);
    }

    private static DeliveryAddress? ToDeliveryAddress(CheckoutAddress? checkoutAddress)
        => checkoutAddress is null
            ? null
            : new DeliveryAddress(
                checkoutAddress.Line1,
                checkoutAddress.Line2,
                checkoutAddress.City,
                checkoutAddress.PostalCode,
                checkoutAddress.CountryCode);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Recorded pending shipment for order {OrderId} (hasPhysicalLines={HasPhysicalLines}, hasShippingAddress={HasShippingAddress})")]
    private partial void LogPendingShipmentRecorded(Guid orderId, bool hasPhysicalLines, bool hasShippingAddress);
}
