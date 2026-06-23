using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Application.Shipments;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FreshCart.Delivery.Tests.Application;

public sealed class BasketCheckoutStartedConsumerTests
{
    private static readonly Guid OrderId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid CustomerId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    private readonly IPendingShipmentRepository pendingShipments = Substitute.For<IPendingShipmentRepository>();
    private readonly BasketCheckoutStartedConsumer consumer;

    public BasketCheckoutStartedConsumerTests() =>
        consumer = new BasketCheckoutStartedConsumer(
            pendingShipments,
            NullLogger<BasketCheckoutStartedConsumer>.Instance);

    [Fact]
    public async Task RecordsADeliverablePendingShipmentWhenTheBasketHasPhysicalLinesAndAShippingAddress()
    {
        var shippingAddress = new CheckoutAddress("9 Park Lane", "Flat 2", "London", "W1K 7TN", "GB");
        var checkout = CreateCheckout(
            shippingAddress,
            new CheckoutLine(Guid.NewGuid(), "SKU-MILK", "Milk", "Dairy", 1.50m, 2, IsDigital: false));

        await consumer.Consume(CreateContext(checkout));

        await pendingShipments.Received(1).UpsertAsync(
            Arg.Is<PendingShipment>(shipment =>
                shipment.OrderId == OrderId
                && shipment.CustomerId == CustomerId
                && shipment.HasPhysicalLines
                && shipment.IsDeliverable
                && shipment.ShippingAddress!.PostalCode == "W1K 7TN"
                && shipment.ShippingAddress!.Line2 == "Flat 2"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordsANonDeliverablePendingShipmentForADigitalOnlyBasket()
    {
        var checkout = CreateCheckout(
            shippingAddress: null,
            new CheckoutLine(Guid.NewGuid(), "SKU-EBOOK", "Cookbook (eBook)", "Books", 9.99m, 1, IsDigital: true));

        await consumer.Consume(CreateContext(checkout));

        await pendingShipments.Received(1).UpsertAsync(
            Arg.Is<PendingShipment>(shipment =>
                !shipment.HasPhysicalLines
                && !shipment.IsDeliverable
                && shipment.ShippingAddress == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TreatsAMixedBasketAsHavingPhysicalLines()
    {
        var shippingAddress = new CheckoutAddress("9 Park Lane", null, "London", "W1K 7TN", "GB");
        var checkout = CreateCheckout(
            shippingAddress,
            new CheckoutLine(Guid.NewGuid(), "SKU-EBOOK", "eBook", "Books", 9.99m, 1, IsDigital: true),
            new CheckoutLine(Guid.NewGuid(), "SKU-APPLES", "Apples", "Produce", 2.00m, 3, IsDigital: false));

        await consumer.Consume(CreateContext(checkout));

        await pendingShipments.Received(1).UpsertAsync(
            Arg.Is<PendingShipment>(shipment => shipment.HasPhysicalLines && shipment.IsDeliverable),
            Arg.Any<CancellationToken>());
    }

    private static BasketCheckoutStartedIntegrationEvent CreateCheckout(
        CheckoutAddress? shippingAddress,
        params CheckoutLine[] lines)
    {
        var billingAddress = new CheckoutAddress("1 Billing Road", null, "London", "EC1A 1BB", "GB");
        return new BasketCheckoutStartedIntegrationEvent
        {
            OrderId = OrderId,
            CustomerId = CustomerId,
            CustomerEmail = "shopper@example.com",
            CustomerDisplayName = "Sam Shopper",
            CurrencyCode = "GBP",
            PaymentMethod = "Card",
            Subtotal = 10m,
            DiscountTotal = 0m,
            TaxTotal = 0.80m,
            ShippingTotal = 3.00m,
            GrandTotal = 13.80m,
            BillingAddress = billingAddress,
            ShippingAddress = shippingAddress,
            Lines = lines,
        };
    }

    private static ConsumeContext<BasketCheckoutStartedIntegrationEvent> CreateContext(
        BasketCheckoutStartedIntegrationEvent checkout)
    {
        var context = Substitute.For<ConsumeContext<BasketCheckoutStartedIntegrationEvent>>();
        context.Message.Returns(checkout);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }
}
