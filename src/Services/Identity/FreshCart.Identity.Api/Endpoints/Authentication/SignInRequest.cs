namespace FreshCart.Identity.Api.Endpoints.Authentication;

/// <summary>
/// Wire shape for the sign-in endpoint; <see cref="UseCookie"/> selects cookie or JWT mode.
/// </summary>
public sealed record SignInRequest(
    string Email,
    string Password,
    string? MultiFactorCode,
    bool UseCookie,
    bool RememberMe);
