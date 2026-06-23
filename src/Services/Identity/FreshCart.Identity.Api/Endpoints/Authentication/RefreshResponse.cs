namespace FreshCart.Identity.Api.Endpoints.Authentication;

/// <summary>
/// Wire shape returned after refresh-token rotation: the replacement credential pair.
/// </summary>
public sealed record RefreshResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresOnUtc);
