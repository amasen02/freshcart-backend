namespace FreshCart.Gateway.Yarp.Auth;

/// <summary>
/// Signs the short-lived bearer token the gateway attaches to proxied requests. Isolated behind an
/// interface so the caching behaviour of <see cref="CookieToJwtTokenExchanger"/> can be asserted by
/// counting calls against a substitute, independently of the real signing cryptography.
/// </summary>
public interface IDownstreamTokenSigner
{
    string SignToken(DownstreamPrincipal principal, DateTimeOffset issuedAt);
}
