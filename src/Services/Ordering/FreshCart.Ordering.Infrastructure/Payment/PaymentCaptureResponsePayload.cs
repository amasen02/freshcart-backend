namespace FreshCart.Ordering.Infrastructure.Payment;

/// <summary>
/// Wire body returned by the Payment service after a capture attempt (its PaymentResultDto). The
/// outcome is the payment Status string ("Captured" on success); a declined card comes back with a
/// non-captured status and a reason rather than as an HTTP error.
/// </summary>
public sealed record PaymentCaptureResponsePayload(
    Guid PaymentId,
    Guid OrderId,
    string Status,
    string? FailureReason);
