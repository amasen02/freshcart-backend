namespace FreshCart.Ordering.Infrastructure.Payment;

/// <summary>
/// Wire body returned by the Payment service after a refund attempt.
/// </summary>
public sealed record PaymentRefundResponsePayload(
    bool Succeeded,
    decimal RefundedAmount,
    string? FailureReason);
