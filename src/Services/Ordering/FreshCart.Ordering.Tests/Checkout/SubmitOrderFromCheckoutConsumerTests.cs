using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Checkout.Commands;
using FreshCart.Ordering.Application.Checkout.Consumers;
using FreshCart.Ordering.Domain.Orders;
using FreshCart.Ordering.Tests.Support;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FreshCart.Ordering.Tests.Checkout;

public sealed class SubmitOrderFromCheckoutConsumerTests
{
    private readonly IOrderRepository orderRepository = Substitute.For<IOrderRepository>();
    private readonly SubmitOrderFromCheckoutConsumer consumer;

    public SubmitOrderFromCheckoutConsumerTests() =>
        consumer = new SubmitOrderFromCheckoutConsumer(orderRepository, NullLogger<SubmitOrderFromCheckoutConsumer>.Instance);

    [Fact]
    public async Task PersistsTheOrderBuiltFromTheCheckoutPayloadOnFirstDelivery()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var context = CreateContext(CheckoutStarted(orderId, customerId));
        orderRepository.ExistsAsync(orderId, Arg.Any<CancellationToken>()).Returns(false);

        await consumer.Consume(context);

        await orderRepository.Received(1).AddAsync(
            Arg.Is<Order>(order => order.Id == orderId && order.CustomerId == customerId && order.Lines.Count == 1),
            Arg.Any<CancellationToken>());
        await orderRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipsPersistenceWhenTheOrderAlreadyExistsSoRedeliveriesDoNotDuplicateIt()
    {
        var orderId = Guid.NewGuid();
        var context = CreateContext(CheckoutStarted(orderId, Guid.NewGuid()));
        orderRepository.ExistsAsync(orderId, Arg.Any<CancellationToken>()).Returns(true);

        await consumer.Consume(context);

        await orderRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await orderRepository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private static ConsumeContext<SubmitOrderFromCheckout> CreateContext(BasketCheckoutStartedIntegrationEvent checkout)
    {
        var context = Substitute.For<ConsumeContext<SubmitOrderFromCheckout>>();
        context.Message.Returns(new SubmitOrderFromCheckout(checkout));
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    private static BasketCheckoutStartedIntegrationEvent CheckoutStarted(Guid orderId, Guid customerId) => new()
    {
        OrderId = orderId,
        CustomerId = customerId,
        CustomerEmail = "shopper@freshcart.local",
        CustomerDisplayName = "Sample Shopper",
        CurrencyCode = OrderTestData.CurrencyCode,
        PaymentMethod = "Card",
        Subtotal = 3.80m,
        DiscountTotal = 0m,
        TaxTotal = 0m,
        ShippingTotal = 0m,
        GrandTotal = 3.80m,
        BillingAddress = new CheckoutAddress("12 Market Street", null, "Springfield", "12345", "US"),
        Lines =
        [
            new CheckoutLine(Guid.NewGuid(), "SKU-MILK-2L", "Full Cream Milk 2L", "Dairy", 3.80m, 1, IsDigital: false),
        ],
    };
}
