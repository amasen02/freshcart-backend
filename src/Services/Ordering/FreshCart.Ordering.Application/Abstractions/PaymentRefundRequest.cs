namespace FreshCart.Ordering.Application.Abstractions;

public sealed record PaymentRefundRequest(
    Guid PaymentId,
    decimal Amount,
    string Reason);
