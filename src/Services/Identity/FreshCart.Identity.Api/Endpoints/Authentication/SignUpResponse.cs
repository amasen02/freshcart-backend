namespace FreshCart.Identity.Api.Endpoints.Authentication;

/// <summary>
/// Wire shape returned after sign-up. Token fields are populated only in JWT mode.
/// </summary>
public sealed record SignUpResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? AccessTokenExpiresOnUtc);
