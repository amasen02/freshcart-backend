namespace FreshCart.Payment.Domain.Events;

public sealed record PaymentInitiated(
    Guid PaymentId,
    int Version,
    DateTimeOffset OccurredOnUtc,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string CurrencyCode,
    string Method) : IPaymentEvent;
