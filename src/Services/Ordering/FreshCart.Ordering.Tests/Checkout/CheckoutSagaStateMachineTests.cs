using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Checkout;
using FreshCart.Ordering.Application.Checkout.Activities;
using FreshCart.Ordering.Application.Checkout.Commands;
using FreshCart.Ordering.Application.Checkout.Consumers;
using FreshCart.Ordering.Domain.Orders;
using FreshCart.Ordering.Tests.Support;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FreshCart.Ordering.Tests.Checkout;

/// <summary>
/// Drives the checkout saga end to end through the MassTransit in-memory harness. The work consumers
/// and aggregate activities run for real against substituted Inventory, Payment and repository ports,
/// so each test asserts both the saga's terminal state and the compensating side effects.
/// </summary>
public sealed class CheckoutSagaStateMachineTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly IOrderRepository orderRepository = Substitute.For<IOrderRepository>();
    private readonly IInventoryClient inventoryClient = Substitute.For<IInventoryClient>();
    private readonly IPaymentClient paymentClient = Substitute.For<IPaymentClient>();

    [Fact]
    public async Task HappyPathReachesConfirmedAndCapturesPayment()
    {
        var orderId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        StubPersistedOrder(OrderTestData.SubmittedOrder(orderId));
        inventoryClient
            .ReserveStockAsync(orderId, Arg.Any<IReadOnlyList<StockReservationLine>>(), Arg.Any<CancellationToken>())
            .Returns(StockReservationResult.Success(reservationId));
        paymentClient
            .CapturePaymentAsync(Arg.Any<PaymentCaptureRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentCaptureResult(Succeeded: true, paymentId, FailureReason: null));

        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(CheckoutStarted(orderId));

        var sagaHarness = harness.GetSagaStateMachineHarness<CheckoutSagaStateMachine, CheckoutState>();

        (await harness.Published.Any<StockReservedIntegrationEvent>()).Should().BeTrue();
        (await sagaHarness.Consumed.Any<StockReservedIntegrationEvent>()).Should().BeTrue();
        (await harness.Published.Any<PaymentCapturedIntegrationEvent>()).Should().BeTrue();
        (await sagaHarness.Consumed.Any<PaymentCapturedIntegrationEvent>()).Should().BeTrue();

        await paymentClient.Received(1).CapturePaymentAsync(Arg.Any<PaymentCaptureRequest>(), Arg.Any<CancellationToken>());
        await inventoryClient.DidNotReceive().ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StockReservationFailureCancelsTheOrderAndFinalizes()
    {
        var orderId = Guid.NewGuid();

        StubPersistedOrder(OrderTestData.SubmittedOrder(orderId));
        inventoryClient
            .ReserveStockAsync(orderId, Arg.Any<IReadOnlyList<StockReservationLine>>(), Arg.Any<CancellationToken>())
            .Returns(StockReservationResult.Failure("Out of stock", ["SKU-MILK-2L"]));

        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(CheckoutStarted(orderId));

        var sagaHarness = harness.GetSagaStateMachineHarness<CheckoutSagaStateMachine, CheckoutState>();

        (await harness.Published.Any<StockReservationFailedIntegrationEvent>()).Should().BeTrue();
        (await sagaHarness.Consumed.Any<StockReservationFailedIntegrationEvent>()).Should().BeTrue();
        (await harness.Published.Any<PaymentCapturedIntegrationEvent>()).Should().BeFalse();

        await paymentClient.DidNotReceive().CapturePaymentAsync(Arg.Any<PaymentCaptureRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PaymentFailureReleasesTheReservationCancelsTheOrderAndFinalizes()
    {
        var orderId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        StubPersistedOrder(OrderTestData.SubmittedOrder(orderId));
        inventoryClient
            .ReserveStockAsync(orderId, Arg.Any<IReadOnlyList<StockReservationLine>>(), Arg.Any<CancellationToken>())
            .Returns(StockReservationResult.Success(reservationId));
        paymentClient
            .CapturePaymentAsync(Arg.Any<PaymentCaptureRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentCaptureResult(Succeeded: false, PaymentId: null, "Card declined"));

        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(CheckoutStarted(orderId));

        var sagaHarness = harness.GetSagaStateMachineHarness<CheckoutSagaStateMachine, CheckoutState>();

        (await harness.Published.Any<PaymentFailedIntegrationEvent>()).Should().BeTrue();
        (await sagaHarness.Consumed.Any<PaymentFailedIntegrationEvent>()).Should().BeTrue();

        await inventoryClient.Received(1).ReleaseReservationAsync(orderId, Arg.Any<CancellationToken>());
    }

    // The substituted repository returns one shared aggregate so the activities advance the same
    // order through its real lifecycle (Submitted -> StockReserved -> Confirmed/Cancelled), exactly
    // as the EF repository would when reloading the persisted row between saga steps.
    private void StubPersistedOrder(Order order) =>
        orderRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(order);

    private ServiceProvider BuildProvider() => new ServiceCollection()
        .AddSingleton(orderRepository)
        .AddSingleton(inventoryClient)
        .AddSingleton(paymentClient)
        .AddSingleton<TimeProvider>(new FixedTimeProvider(Now))
        .AddScoped<MarkOrderStockReservedActivity>()
        .AddScoped<ConfirmOrderActivity>()
        .AddScoped<CancelOrderOnStockFailureActivity>()
        .AddScoped<CancelOrderOnPaymentFailureActivity>()
        .AddMassTransitTestHarness(massTransit =>
        {
            massTransit.AddConsumer<SubmitOrderFromCheckoutConsumer>();
            massTransit.AddConsumer<ReserveOrderStockConsumer>();
            massTransit.AddConsumer<CaptureOrderPaymentConsumer>();
            massTransit.AddSagaStateMachine<CheckoutSagaStateMachine, CheckoutState>();
        })
        .BuildServiceProvider(validateScopes: true);

    private static BasketCheckoutStartedIntegrationEvent CheckoutStarted(Guid orderId) => new()
    {
        OrderId = orderId,
        CustomerId = Guid.NewGuid(),
        CustomerEmail = "shopper@freshcart.local",
        CustomerDisplayName = "Sample Shopper",
        CurrencyCode = OrderTestData.CurrencyCode,
        PaymentMethod = "Card",
        Subtotal = 12.80m,
        DiscountTotal = 2.00m,
        TaxTotal = 1.10m,
        ShippingTotal = 5.00m,
        GrandTotal = 16.90m,
        BillingAddress = new CheckoutAddress("12 Market Street", null, "Springfield", "12345", "US"),
        Lines =
        [
            new CheckoutLine(Guid.NewGuid(), "SKU-MILK-2L", "Full Cream Milk 2L", "Dairy", 3.80m, 1, IsDigital: false),
        ],
    };
}
