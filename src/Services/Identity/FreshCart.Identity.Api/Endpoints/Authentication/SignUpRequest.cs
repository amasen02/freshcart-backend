namespace FreshCart.Identity.Api.Endpoints.Authentication;

/// <summary>
/// Wire shape for the sign-up endpoint. <see cref="UseCookie"/> selects cookie-mode session
/// creation when combined with <see cref="SignInImmediately"/>.
/// </summary>
public sealed record SignUpRequest(
    string Email,
    string Password,
    string DisplayName,
    bool MarketingConsent,
    bool SignInImmediately,
    bool UseCookie);
