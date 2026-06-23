using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Checkout.Commands;
using FreshCart.Ordering.Application.Checkout.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FreshCart.Ordering.Tests.Checkout;

public sealed class ReserveOrderStockConsumerTests
{
    private readonly IInventoryClient inventoryClient = Substitute.For<IInventoryClient>();
    private readonly ReserveOrderStockConsumer consumer;

    public ReserveOrderStockConsumerTests() =>
        consumer = new ReserveOrderStockConsumer(inventoryClient, NullLogger<ReserveOrderStockConsumer>.Instance);

    [Fact]
    public async Task PublishesStockReservedWhenInventoryGrantsTheReservation()
    {
        var orderId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var context = CreateContext(orderId);

        inventoryClient
            .ReserveStockAsync(orderId, Arg.Any<IReadOnlyList<StockReservationLine>>(), Arg.Any<CancellationToken>())
            .Returns(StockReservationResult.Success(reservationId));

        await consumer.Consume(context);

        await context.Received(1).Publish(
            Arg.Is<StockReservedIntegrationEvent>(reserved =>
                reserved.OrderId == orderId && reserved.ReservationId == reservationId));
    }

    [Fact]
    public async Task PublishesStockReservationFailedWithUnavailableSkusWhenInventoryRejects()
    {
        var orderId = Guid.NewGuid();
        var context = CreateContext(orderId);

        inventoryClient
            .ReserveStockAsync(orderId, Arg.Any<IReadOnlyList<StockReservationLine>>(), Arg.Any<CancellationToken>())
            .Returns(StockReservationResult.Failure("Out of stock", ["SKU-MILK-2L"]));

        await consumer.Consume(context);

        await context.Received(1).Publish(
            Arg.Is<StockReservationFailedIntegrationEvent>(failed =>
                failed.OrderId == orderId
                && failed.Reason == "Out of stock"
                && failed.UnavailableSkus.Contains("SKU-MILK-2L")));
        await context.DidNotReceive().Publish(Arg.Any<StockReservedIntegrationEvent>());
    }

    private static ConsumeContext<ReserveOrderStock> CreateContext(Guid orderId)
    {
        var lines = new List<CheckoutLine>
        {
            new(Guid.NewGuid(), "SKU-MILK-2L", "Full Cream Milk 2L", "Dairy", 3.80m, 1, IsDigital: false),
        };

        var context = Substitute.For<ConsumeContext<ReserveOrderStock>>();
        context.Message.Returns(new ReserveOrderStock(orderId, lines));
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }
}
