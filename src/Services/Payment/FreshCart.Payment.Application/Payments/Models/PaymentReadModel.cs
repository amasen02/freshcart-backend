using FreshCart.Payment.Domain;

namespace FreshCart.Payment.Application.Payments.Models;

/// <summary>
/// Flattened projection of a payment stream, one row per payment in the SQL read model.
/// </summary>
public sealed record PaymentReadModel(
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    decimal RefundedAmount,
    string CurrencyCode,
    string Method,
    PaymentStatus Status,
    string? ProviderReference,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset UpdatedOnUtc)
{
    public static PaymentReadModel FromAggregate(PaymentAggregate payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return new PaymentReadModel(
            payment.PaymentId,
            payment.OrderId,
            payment.CustomerId,
            payment.Amount,
            payment.RefundedAmount,
            payment.CurrencyCode,
            payment.Method,
            payment.Status,
            payment.ProviderReference,
            payment.InitiatedOnUtc,
            payment.LastChangedOnUtc);
    }
}
