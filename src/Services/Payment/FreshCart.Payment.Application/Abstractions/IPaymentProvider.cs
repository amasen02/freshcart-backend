namespace FreshCart.Payment.Application.Abstractions;

/// <summary>
/// Port to the external card processor. A declined operation is a regular result, never an
/// exception; the caller decides what a decline means for the payment stream.
/// </summary>
public interface IPaymentProvider
{
    Task<ProviderAuthorizationResult> AuthorizeAsync(
        ProviderAuthorizationRequest authorizationRequest,
        CancellationToken cancellationToken);

    Task<ProviderCaptureResult> CaptureAsync(
        string providerReference,
        decimal amount,
        string currencyCode,
        CancellationToken cancellationToken);

    Task<ProviderRefundResult> RefundAsync(
        string providerReference,
        decimal amount,
        string currencyCode,
        CancellationToken cancellationToken);
}
