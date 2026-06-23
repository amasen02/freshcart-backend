namespace FreshCart.Identity.Application.Authentication.Commands.RefreshAccessToken;

/// <summary>
/// New credential pair produced by a successful refresh-token rotation.
/// </summary>
public sealed record RefreshAccessTokenResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresOnUtc);
