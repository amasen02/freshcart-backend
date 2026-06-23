namespace FreshCart.Payment.Domain.Events;

public sealed record PaymentDeclined(
    Guid PaymentId,
    int Version,
    DateTimeOffset OccurredOnUtc,
    string Reason) : IPaymentEvent;
