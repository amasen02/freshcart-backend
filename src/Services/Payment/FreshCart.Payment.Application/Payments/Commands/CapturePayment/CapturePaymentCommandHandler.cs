using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Domain;
using Microsoft.Extensions.Logging;

namespace FreshCart.Payment.Application.Payments.Commands.CapturePayment;

/// <summary>
/// Runs the full Initiate, Authorize, Capture sequence against the provider, appending an event after
/// every step. Idempotent per order: the idempotency check reads the event store — the source of truth —
/// so a payment whose read-model projection has not caught up yet is still never captured twice. When a
/// stream already exists for the order the recorded outcome is replayed and returned without touching the
/// provider. A declined card is a domain outcome carried in the result, not an exception. The SQL read
/// model is no longer written in the request path; the event append stages a projection intent in the
/// same transaction and a background projector applies it, closing the former Mongo+SQL dual write.
/// </summary>
public sealed partial class CapturePaymentCommandHandler(
    IPaymentEventStore paymentEventStore,
    IPaymentProvider paymentProvider,
    TimeProvider timeProvider,
    ILogger<CapturePaymentCommandHandler> logger)
    : ICommandHandler<CapturePaymentCommand, CapturePaymentResult>
{
    public async Task<CapturePaymentResult> Handle(
        CapturePaymentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existingPaymentId = await paymentEventStore
            .FindStreamIdByOrderIdAsync(command.OrderId, cancellationToken)
            .ConfigureAwait(false);

        return existingPaymentId is null
            ? await CaptureNewPaymentAsync(command, cancellationToken).ConfigureAwait(false)
            : await ReplayRecordedOutcomeAsync(existingPaymentId.Value, command.OrderId, cancellationToken)
                .ConfigureAwait(false);
    }

    private async Task<CapturePaymentResult> CaptureNewPaymentAsync(
        CapturePaymentCommand command,
        CancellationToken cancellationToken)
    {
        var initiatedOnUtc = timeProvider.GetUtcNow();
        var paymentId = Guid.CreateVersion7(initiatedOnUtc);
        var payment = PaymentAggregate.Initiate(
            paymentId,
            command.OrderId,
            command.CustomerId,
            command.Amount,
            command.CurrencyCode,
            command.Method,
            initiatedOnUtc);

        await AppendUncommittedEventsAsync(payment, cancellationToken).ConfigureAwait(false);

        var authorization = await paymentProvider
            .AuthorizeAsync(
                new ProviderAuthorizationRequest(
                    paymentId,
                    command.OrderId,
                    command.Amount,
                    command.CurrencyCode,
                    command.Method),
                cancellationToken)
            .ConfigureAwait(false);

        if (!authorization.IsApproved)
        {
            return await DeclineAsync(payment, authorization.DeclineReason, cancellationToken)
                .ConfigureAwait(false);
        }

        var providerReference = authorization.ProviderReference
            ?? throw new InternalServerException("The payment provider approved an authorization without a provider reference.");

        payment.Authorize(providerReference, timeProvider.GetUtcNow());
        await AppendUncommittedEventsAsync(payment, cancellationToken).ConfigureAwait(false);

        var capture = await paymentProvider
            .CaptureAsync(providerReference, command.Amount, command.CurrencyCode, cancellationToken)
            .ConfigureAwait(false);

        if (!capture.IsApproved)
        {
            return await DeclineAsync(payment, capture.DeclineReason, cancellationToken)
                .ConfigureAwait(false);
        }

        payment.Capture(timeProvider.GetUtcNow());
        await AppendUncommittedEventsAsync(payment, cancellationToken).ConfigureAwait(false);

        LogPaymentCaptured(payment.PaymentId, payment.OrderId, payment.Amount, payment.CurrencyCode);

        return new CapturePaymentResult(payment.PaymentId, payment.OrderId, payment.Status, FailureReason: null);
    }

    private async Task<CapturePaymentResult> ReplayRecordedOutcomeAsync(
        Guid paymentId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var eventStream = await paymentEventStore
            .LoadStreamAsync(paymentId, cancellationToken)
            .ConfigureAwait(false);

        var payment = PaymentAggregate.ReplayFrom(eventStream);

        LogIdempotentReplay(payment.PaymentId, orderId, payment.Status);

        return new CapturePaymentResult(payment.PaymentId, payment.OrderId, payment.Status, payment.DeclineReason);
    }

    private async Task<CapturePaymentResult> DeclineAsync(
        PaymentAggregate payment,
        string? providerDeclineReason,
        CancellationToken cancellationToken)
    {
        var declineReason = providerDeclineReason
            ?? throw new InternalServerException("The payment provider declined an operation without a reason.");

        payment.Decline(declineReason, timeProvider.GetUtcNow());
        await AppendUncommittedEventsAsync(payment, cancellationToken).ConfigureAwait(false);

        LogPaymentDeclined(payment.PaymentId, payment.OrderId, declineReason);

        return new CapturePaymentResult(payment.PaymentId, payment.OrderId, payment.Status, declineReason);
    }

    private Task AppendUncommittedEventsAsync(PaymentAggregate payment, CancellationToken cancellationToken)
    {
        var uncommittedEvents = payment.DequeueUncommittedEvents();
        var expectedVersion = payment.Version - uncommittedEvents.Count;

        return paymentEventStore
            .AppendAsync(payment.OrderId, payment.PaymentId, expectedVersion, uncommittedEvents, cancellationToken);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Captured payment {PaymentId} for order {OrderId} ({Amount} {CurrencyCode})")]
    private partial void LogPaymentCaptured(Guid paymentId, Guid orderId, decimal amount, string currencyCode);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Payment {PaymentId} already exists for order {OrderId}; returning the recorded {Status} outcome")]
    private partial void LogIdempotentReplay(Guid paymentId, Guid orderId, PaymentStatus status);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Declined payment {PaymentId} for order {OrderId}: {DeclineReason}")]
    private partial void LogPaymentDeclined(Guid paymentId, Guid orderId, string declineReason);
}
