namespace FreshCart.Payment.Domain.Events;

public sealed record PaymentAuthorized(
    Guid PaymentId,
    int Version,
    DateTimeOffset OccurredOnUtc,
    string ProviderReference) : IPaymentEvent;
