namespace FreshCart.Ordering.Infrastructure.Payment;

/// <summary>
/// Wire body posted to the Payment service to capture a charge. The order id travels in the
/// Idempotency-Key header rather than the body so the Payment service deduplicates on the same key
/// regardless of body shape.
/// </summary>
public sealed record PaymentCaptureRequestPayload(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string CurrencyCode,
    string PaymentMethod);
