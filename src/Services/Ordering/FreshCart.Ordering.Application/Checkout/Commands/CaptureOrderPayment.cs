namespace FreshCart.Ordering.Application.Checkout.Commands;

/// <summary>
/// Saga-internal command (never leaves the Ordering service) instructing a worker to capture the
/// order payment with the Payment service. The order id doubles as the idempotency key.
/// </summary>
public sealed record CaptureOrderPayment(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string CurrencyCode,
    string PaymentMethod);
