using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Checkout.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Ordering.Application.Checkout.Consumers;

/// <summary>
/// Captures the order payment through the Payment service, then publishes the outcome as the
/// integration event the saga correlates on. The order id is the idempotency key, so a redelivered
/// command never double-charges the customer. A declined card is a business result and comes back as
/// a published failure event; a transport fault throws so the bus retry policy re-runs the capture.
/// </summary>
public sealed partial class CaptureOrderPaymentConsumer(
    IPaymentClient paymentClient,
    ILogger<CaptureOrderPaymentConsumer> logger) : IConsumer<CaptureOrderPayment>
{
    public async Task Consume(ConsumeContext<CaptureOrderPayment> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var command = context.Message;

        var captureRequest = new PaymentCaptureRequest(
            command.OrderId,
            command.CustomerId,
            command.Amount,
            command.CurrencyCode,
            command.PaymentMethod);

        var capture = await paymentClient
            .CapturePaymentAsync(captureRequest, context.CancellationToken)
            .ConfigureAwait(false);

        if (capture.Succeeded && capture.PaymentId.HasValue)
        {
            LogPaymentCaptured(command.OrderId, capture.PaymentId.Value, command.Amount, command.CurrencyCode);

            await context.Publish(new PaymentCapturedIntegrationEvent
            {
                OrderId = command.OrderId,
                PaymentId = capture.PaymentId.Value,
                Amount = command.Amount,
                CurrencyCode = command.CurrencyCode,
                PaymentMethod = command.PaymentMethod,
            }).ConfigureAwait(false);

            return;
        }

        var reason = capture.FailureReason ?? "The payment provider declined the charge.";
        LogPaymentFailed(command.OrderId, reason);

        await context.Publish(new PaymentFailedIntegrationEvent
        {
            OrderId = command.OrderId,
            PaymentId = capture.PaymentId,
            Reason = reason,
        }).ConfigureAwait(false);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Captured payment {PaymentId} for order {OrderId} totalling {Amount} {CurrencyCode}")]
    private partial void LogPaymentCaptured(Guid orderId, Guid paymentId, decimal amount, string currencyCode);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Payment failed for order {OrderId}: {Reason}")]
    private partial void LogPaymentFailed(Guid orderId, string reason);
}
