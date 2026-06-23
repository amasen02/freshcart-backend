namespace FreshCart.Ordering.Infrastructure.Payment;

/// <summary>
/// Wire body returned by the Payment service after a capture attempt. A declined card comes back as
/// <c>Succeeded = false</c> with a reason rather than as an HTTP error.
/// </summary>
public sealed record PaymentCaptureResponsePayload(
    bool Succeeded,
    Guid? PaymentId,
    string? FailureReason);
