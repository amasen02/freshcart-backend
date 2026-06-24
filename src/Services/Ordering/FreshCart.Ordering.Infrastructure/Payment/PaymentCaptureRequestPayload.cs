namespace FreshCart.Ordering.Infrastructure.Payment;

/// <summary>
/// Wire body posted to the Payment service to capture a charge. The order id travels in the
/// Idempotency-Key header rather than the body so the Payment service deduplicates on the same key
/// regardless of body shape.
/// </summary>
// The last field must be named Method (not PaymentMethod): the Payment service's CapturePaymentRequest
// binds a "Method" property, so the JSON keys have to match or capture fails validation ("Method must
// not be empty").
public sealed record PaymentCaptureRequestPayload(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string CurrencyCode,
    string Method);
