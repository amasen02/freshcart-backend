namespace FreshCart.Identity.Api.Endpoints.Authentication;

/// <summary>
/// Wire shape returned after sign-in. Token fields are populated only in JWT mode.
/// </summary>
public sealed record SignInResponse(
    AuthenticationProfileDto Profile,
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? AccessTokenExpiresOnUtc);
