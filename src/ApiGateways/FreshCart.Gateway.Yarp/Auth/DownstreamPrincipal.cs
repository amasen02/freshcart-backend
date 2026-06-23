namespace FreshCart.Gateway.Yarp.Auth;

/// <summary>
/// The minimal identity the gateway projects onto the downstream bearer token. Extracted from the
/// cookie principal so the signer never touches <see cref="System.Security.Claims.ClaimsPrincipal"/>
/// directly and stays trivially testable.
/// </summary>
public sealed record DownstreamPrincipal(
    string Subject,
    string? Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
