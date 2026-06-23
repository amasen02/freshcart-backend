namespace FreshCart.Ordering.Application.Abstractions;

public sealed record PaymentCaptureRequest(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string CurrencyCode,
    string PaymentMethod);
