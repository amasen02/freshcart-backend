namespace FreshCart.Identity.Api.Endpoints.Account;

/// <summary>
/// One-time disclosure of the recovery codes generated when multi-factor is enabled.
/// </summary>
public sealed record VerifyMultiFactorEnrollmentResponse(IReadOnlyCollection<string> RecoveryCodes);
