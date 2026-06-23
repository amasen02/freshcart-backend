namespace FreshCart.Payment.Application.Abstractions;

public sealed record ProviderAuthorizationResult(
    bool IsApproved,
    string? ProviderReference,
    string? DeclineReason)
{
    public static ProviderAuthorizationResult Approved(string providerReference) =>
        new(true, providerReference, null);

    public static ProviderAuthorizationResult Declined(string declineReason) =>
        new(false, null, declineReason);
}
