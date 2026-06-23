namespace FreshCart.Payment.Domain.Events;

public sealed record PaymentCaptured(
    Guid PaymentId,
    int Version,
    DateTimeOffset OccurredOnUtc) : IPaymentEvent;
