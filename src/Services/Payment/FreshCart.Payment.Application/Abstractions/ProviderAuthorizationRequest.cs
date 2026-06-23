namespace FreshCart.Payment.Application.Abstractions;

public sealed record ProviderAuthorizationRequest(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string CurrencyCode,
    string Method);
