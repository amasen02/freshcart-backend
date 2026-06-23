using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Identity.Application.Authentication.Commands.RefreshAccessToken;

/// <summary>
/// Exchanges a refresh token for a new short-lived access token. Used by JWT-mode clients only;
/// browser sessions are renewed via cookie sliding expiration handled by the cookie middleware.
/// </summary>
public sealed record RefreshAccessTokenCommand(string RefreshToken) : ICommand<RefreshAccessTokenResult>;
