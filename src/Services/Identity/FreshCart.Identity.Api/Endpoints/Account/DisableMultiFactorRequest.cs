namespace FreshCart.Identity.Api.Endpoints.Account;

/// <summary>
/// TOTP code required to prove possession of the authenticator before multi-factor is disabled.
/// </summary>
public sealed record DisableMultiFactorRequest(string VerificationCode);
