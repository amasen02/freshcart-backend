namespace FreshCart.Ordering.Infrastructure.Payment;

/// <summary>
/// Wire body returned by the Payment service after a refund attempt (its RefundResultDto). The outcome
/// is the payment Status string ("Refunded" / "PartiallyRefunded" on success).
/// </summary>
public sealed record PaymentRefundResponsePayload(
    Guid PaymentId,
    Guid OrderId,
    string Status,
    decimal RefundedAmount);
