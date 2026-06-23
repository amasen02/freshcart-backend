namespace FreshCart.Payment.Api.Endpoints;

public sealed record CapturePaymentRequest(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string CurrencyCode,
    string Method);
