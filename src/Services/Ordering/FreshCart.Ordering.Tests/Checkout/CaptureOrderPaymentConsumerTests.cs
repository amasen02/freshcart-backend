using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Checkout.Commands;
using FreshCart.Ordering.Application.Checkout.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FreshCart.Ordering.Tests.Checkout;

public sealed class CaptureOrderPaymentConsumerTests
{
    private const string CurrencyCode = "USD";
    private const string PaymentMethod = "Card";

    private readonly IPaymentClient paymentClient = Substitute.For<IPaymentClient>();
    private readonly CaptureOrderPaymentConsumer consumer;

    public CaptureOrderPaymentConsumerTests() =>
        consumer = new CaptureOrderPaymentConsumer(paymentClient, NullLogger<CaptureOrderPaymentConsumer>.Instance);

    [Fact]
    public async Task PublishesPaymentCapturedWhenTheProviderAcceptsTheCharge()
    {
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var context = CreateContext(orderId, 16.90m);

        paymentClient
            .CapturePaymentAsync(Arg.Any<PaymentCaptureRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentCaptureResult(Succeeded: true, paymentId, FailureReason: null));

        await consumer.Consume(context);

        await context.Received(1).Publish(
            Arg.Is<PaymentCapturedIntegrationEvent>(captured =>
                captured.OrderId == orderId
                && captured.PaymentId == paymentId
                && captured.Amount == 16.90m
                && captured.CurrencyCode == CurrencyCode));
    }

    [Fact]
    public async Task PublishesPaymentFailedWhenTheProviderDeclinesTheCard()
    {
        var orderId = Guid.NewGuid();
        var context = CreateContext(orderId, 16.90m);

        paymentClient
            .CapturePaymentAsync(Arg.Any<PaymentCaptureRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentCaptureResult(Succeeded: false, PaymentId: null, "Card declined"));

        await consumer.Consume(context);

        await context.Received(1).Publish(
            Arg.Is<PaymentFailedIntegrationEvent>(failed =>
                failed.OrderId == orderId && failed.Reason == "Card declined"));
        await context.DidNotReceive().Publish(Arg.Any<PaymentCapturedIntegrationEvent>());
    }

    private static ConsumeContext<CaptureOrderPayment> CreateContext(Guid orderId, decimal amount)
    {
        var context = Substitute.For<ConsumeContext<CaptureOrderPayment>>();
        context.Message.Returns(new CaptureOrderPayment(orderId, Guid.NewGuid(), amount, CurrencyCode, PaymentMethod));
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }
}
