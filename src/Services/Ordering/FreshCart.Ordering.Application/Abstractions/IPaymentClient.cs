namespace FreshCart.Ordering.Application.Abstractions;

/// <summary>
/// Port over the Payment REST API. A declined card is a business outcome and comes back as a
/// result; transport and contract faults propagate as exceptions so callers can retry.
/// </summary>
public interface IPaymentClient
{
    Task<PaymentCaptureResult> CapturePaymentAsync(
        PaymentCaptureRequest captureRequest,
        CancellationToken cancellationToken);

    Task<PaymentRefundResult> RefundPaymentAsync(
        PaymentRefundRequest refundRequest,
        CancellationToken cancellationToken);
}
