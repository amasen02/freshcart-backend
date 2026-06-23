namespace FreshCart.Ordering.Application.Abstractions;

public sealed record PaymentRefundResult(
    bool Succeeded,
    decimal RefundedAmount,
    string? FailureReason);
