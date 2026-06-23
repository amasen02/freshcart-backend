namespace FreshCart.Identity.Api.Endpoints.Account;

/// <summary>
/// TOTP code proving the user captured the shared key issued at enrollment start.
/// </summary>
public sealed record VerifyMultiFactorEnrollmentRequest(string VerificationCode);
