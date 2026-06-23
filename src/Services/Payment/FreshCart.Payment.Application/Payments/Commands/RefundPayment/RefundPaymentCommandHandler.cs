using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Application.Payments.Models;
using FreshCart.Payment.Domain;
using Microsoft.Extensions.Logging;

namespace FreshCart.Payment.Application.Payments.Commands.RefundPayment;

/// <summary>
/// Refunds part or all of a captured payment. Domain invariants are checked first so an invalid
/// refund never reaches the provider; the refund event is only persisted after the provider
/// approves, so a provider rejection leaves the stream untouched.
/// </summary>
public sealed partial class RefundPaymentCommandHandler(
    IPaymentEventStore paymentEventStore,
    IPaymentReadModelWriter paymentReadModelWriter,
    IPaymentProvider paymentProvider,
    TimeProvider timeProvider,
    ILogger<RefundPaymentCommandHandler> logger)
    : ICommandHandler<RefundPaymentCommand, RefundPaymentResult>
{
    public async Task<RefundPaymentResult> Handle(
        RefundPaymentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var eventStream = await paymentEventStore
            .LoadStreamAsync(command.PaymentId, cancellationToken)
            .ConfigureAwait(false);

        if (eventStream.Count == 0)
        {
            throw new NotFoundException("Payment", command.PaymentId);
        }

        var payment = PaymentAggregate.ReplayFrom(eventStream);
        payment.Refund(command.Amount, command.Reason, timeProvider.GetUtcNow());

        var providerReference = payment.ProviderReference
            ?? throw new InternalServerException($"Captured payment {payment.PaymentId} is missing its provider reference.");

        var refund = await paymentProvider
            .RefundAsync(providerReference, command.Amount, payment.CurrencyCode, cancellationToken)
            .ConfigureAwait(false);

        if (!refund.IsApproved)
        {
            throw new BadRequestException(
                "The payment provider rejected the refund.",
                refund.DeclineReason ?? "The provider gave no reason.");
        }

        await AppendAndProjectAsync(payment, cancellationToken).ConfigureAwait(false);

        LogPaymentRefunded(command.Amount, payment.CurrencyCode, payment.PaymentId, payment.OrderId, payment.Status);

        return new RefundPaymentResult(payment.PaymentId, payment.OrderId, payment.Status, payment.RefundedAmount);
    }

    private async Task AppendAndProjectAsync(PaymentAggregate payment, CancellationToken cancellationToken)
    {
        var uncommittedEvents = payment.DequeueUncommittedEvents();
        var expectedVersion = payment.Version - uncommittedEvents.Count;

        await paymentEventStore
            .AppendAsync(payment.PaymentId, expectedVersion, uncommittedEvents, cancellationToken)
            .ConfigureAwait(false);

        await paymentReadModelWriter
            .UpsertAsync(PaymentReadModel.FromAggregate(payment), cancellationToken)
            .ConfigureAwait(false);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Refunded {Amount} {CurrencyCode} on payment {PaymentId} for order {OrderId}; status is now {Status}")]
    private partial void LogPaymentRefunded(decimal amount, string currencyCode, Guid paymentId, Guid orderId, PaymentStatus status);
}
