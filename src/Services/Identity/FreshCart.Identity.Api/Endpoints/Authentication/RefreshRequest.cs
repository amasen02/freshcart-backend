namespace FreshCart.Identity.Api.Endpoints.Authentication;

/// <summary>
/// Wire shape carrying the refresh token to rotate.
/// </summary>
public sealed record RefreshRequest(string RefreshToken);
