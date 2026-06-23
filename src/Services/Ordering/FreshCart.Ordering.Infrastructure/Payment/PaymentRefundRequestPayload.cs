namespace FreshCart.Ordering.Infrastructure.Payment;

/// <summary>
/// Wire body posted to the Payment service to refund a captured payment.
/// </summary>
public sealed record PaymentRefundRequestPayload(decimal Amount, string Reason);
