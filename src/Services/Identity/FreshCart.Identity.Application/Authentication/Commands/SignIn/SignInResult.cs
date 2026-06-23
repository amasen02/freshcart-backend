using FreshCart.Identity.Application.Common.Models;

namespace FreshCart.Identity.Application.Authentication.Commands.SignIn;

/// <summary>
/// Outcome of a successful sign-in. Token fields are populated only in JWT mode; cookie-mode
/// callers receive the profile alone because the session lives in the HttpOnly cookie.
/// </summary>
public sealed record SignInResult(
    AuthenticationProfile Profile,
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? AccessTokenExpiresOnUtc);
