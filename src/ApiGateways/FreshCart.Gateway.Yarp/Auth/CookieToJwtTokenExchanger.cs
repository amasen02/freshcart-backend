using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace FreshCart.Gateway.Yarp.Auth;

/// <summary>
/// Turns the cookie-authenticated principal into the short-lived bearer token the gateway forwards
/// downstream. This is the BFF trust boundary: the browser holds an opaque session cookie, services
/// only ever see a JWT. The minted token is cached per identity so a burst of proxied requests does
/// not re-run the signing cryptography on every hop; the cache entry lives shorter than the token so
/// a cached token is always comfortably inside its validity window.
/// </summary>
public sealed class CookieToJwtTokenExchanger
{
    private static readonly TimeSpan CachedTokenLifetime = TimeSpan.FromMinutes(4);
    private const string CacheKeyPrefix = "freshcart:gateway:downstream-token:";
    private const string AuthenticationInstantClaim = "auth_time";

    private readonly IDownstreamTokenSigner tokenSigner;
    private readonly IMemoryCache tokenCache;
    private readonly TimeProvider timeProvider;

    public CookieToJwtTokenExchanger(
        IDownstreamTokenSigner tokenSigner,
        IMemoryCache tokenCache,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(tokenSigner);
        ArgumentNullException.ThrowIfNull(tokenCache);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.tokenSigner = tokenSigner;
        this.tokenCache = tokenCache;
        this.timeProvider = timeProvider;
    }

    public string ExchangeForBearerToken(ClaimsPrincipal cookiePrincipal)
    {
        ArgumentNullException.ThrowIfNull(cookiePrincipal);

        var downstreamPrincipal = MapPrincipal(cookiePrincipal);
        var cacheKey = BuildCacheKey(downstreamPrincipal.Subject, ResolveAuthenticationInstant(cookiePrincipal));

        if (tokenCache.TryGetValue(cacheKey, out string? cachedToken) && cachedToken is not null)
        {
            return cachedToken;
        }

        var signedToken = tokenSigner.SignToken(downstreamPrincipal, timeProvider.GetUtcNow());
        tokenCache.Set(cacheKey, signedToken, CachedTokenLifetime);
        return signedToken;
    }

    private static DownstreamPrincipal MapPrincipal(ClaimsPrincipal cookiePrincipal)
    {
        var subject = cookiePrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? cookiePrincipal.FindFirstValue(DownstreamTokenClaim.Subject)
            ?? throw new InvalidOperationException("Cookie principal carries no subject claim.");

        var email = cookiePrincipal.FindFirstValue(ClaimTypes.Email);
        var displayName = cookiePrincipal.FindFirstValue(ClaimTypes.Name);

        var roles = cookiePrincipal.FindAll(ClaimTypes.Role)
            .Select(roleClaim => roleClaim.Value)
            .ToArray();

        return new DownstreamPrincipal(subject, email, displayName, roles);
    }

    // The authentication instant changes on every fresh sign-in, so keying the cache on it guarantees
    // a re-authenticated principal mints a new token instead of replaying a stale one for the old
    // session. An absent instant must fail loudly rather than collapse to a stable key: a constant key
    // would let a token minted for a prior session be replayed for a freshly re-authenticated one.
    private static string ResolveAuthenticationInstant(ClaimsPrincipal cookiePrincipal) =>
        cookiePrincipal.FindFirstValue(AuthenticationInstantClaim)
            ?? cookiePrincipal.FindFirstValue(ClaimTypes.AuthenticationInstant)
            ?? throw new InvalidOperationException(
                "Cookie principal carries no authentication instant; cannot build a session-bound cache key.");

    private static string BuildCacheKey(string subject, string authenticationInstant) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{CacheKeyPrefix}{subject}:{authenticationInstant}");
}
