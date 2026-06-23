using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace FreshCart.Gateway.Yarp.Auth;

/// <summary>
/// Decides whether the gateway should mint and attach a downstream bearer token for a given inbound
/// request. Kept separate from the YARP transform so the rule is unit-testable without constructing
/// proxy transform plumbing: a request that already carries an Authorization header is a programmatic
/// client and passes through untouched; an anonymous request gets nothing; a cookie-authenticated
/// request is exchanged.
/// </summary>
public static class DownstreamAuthorizationPolicy
{
    public static bool ShouldExchangeCookieForBearerToken(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            return false;
        }

        return httpContext.User.Identity is { IsAuthenticated: true };
    }

    public static string BuildAuthorizationHeaderValue(string bearerToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);

        return $"Bearer {bearerToken}";
    }
}
