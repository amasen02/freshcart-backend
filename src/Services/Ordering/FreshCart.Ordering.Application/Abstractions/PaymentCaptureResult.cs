namespace FreshCart.Ordering.Application.Abstractions;

public sealed record PaymentCaptureResult(
    bool Succeeded,
    Guid? PaymentId,
    string? FailureReason);
