using FreshCart.Payment.Application.Abstractions;

namespace FreshCart.Payment.Infrastructure.Providers;

/// <summary>
/// Deterministic stand-in for a real card processor; production adapters (Stripe, Adyen)
/// implement the same <see cref="IPaymentProvider"/> port and swap in via DI.
/// </summary>
public sealed class SimulatedCardPaymentProvider : IPaymentProvider
{
    public const string DeclinedTestMethod = "card-declined";
    public const decimal AuthorizationCeiling = 10_000m;
    public const string IssuerDeclineReason = "The card was declined by the issuing bank.";
    public const string OverLimitDeclineReason = "The amount exceeds the single-transaction authorization limit.";

    private const string ProviderReferencePrefix = "SIM-";

    public Task<ProviderAuthorizationResult> AuthorizeAsync(
        ProviderAuthorizationRequest authorizationRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorizationRequest);

        if (string.Equals(authorizationRequest.Method, DeclinedTestMethod, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(ProviderAuthorizationResult.Declined(IssuerDeclineReason));
        }

        if (authorizationRequest.Amount > AuthorizationCeiling)
        {
            return Task.FromResult(ProviderAuthorizationResult.Declined(OverLimitDeclineReason));
        }

        var providerReference = $"{ProviderReferencePrefix}{authorizationRequest.PaymentId:N}";
        return Task.FromResult(ProviderAuthorizationResult.Approved(providerReference));
    }

    public Task<ProviderCaptureResult> CaptureAsync(
        string providerReference,
        decimal amount,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerReference);

        return Task.FromResult(ProviderCaptureResult.Approved());
    }

    public Task<ProviderRefundResult> RefundAsync(
        string providerReference,
        decimal amount,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerReference);

        return Task.FromResult(ProviderRefundResult.Approved());
    }
}
