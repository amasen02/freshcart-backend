using FreshCart.Payment.Application.Payments.Models;

namespace FreshCart.Payment.Api.Endpoints;

public sealed record PaymentResponse(
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    decimal RefundedAmount,
    string CurrencyCode,
    string Method,
    string Status,
    string? ProviderReference,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset UpdatedOnUtc)
{
    public static PaymentResponse FromReadModel(PaymentReadModel payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return new PaymentResponse(
            payment.PaymentId,
            payment.OrderId,
            payment.CustomerId,
            payment.Amount,
            payment.RefundedAmount,
            payment.CurrencyCode,
            payment.Method,
            payment.Status.ToString(),
            payment.ProviderReference,
            payment.CreatedOnUtc,
            payment.UpdatedOnUtc);
    }
}
