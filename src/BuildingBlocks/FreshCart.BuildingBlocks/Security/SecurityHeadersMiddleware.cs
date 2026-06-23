using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FreshCart.BuildingBlocks.Security;

/// <summary>
/// Applies a strict set of security response headers. Centralising the headers prevents each service
/// from rolling its own (inconsistent) approach.
/// </summary>
public static class SecurityHeadersMiddleware
{
    /// <summary>
    /// Adds the OWASP-recommended security headers to every response.
    /// Call before <c>UseStaticFiles</c> / <c>UseRouting</c>.
    /// </summary>
    public static IApplicationBuilder UseFreshCartSecurityHeaders(this IApplicationBuilder applicationBuilder)
    {
        ArgumentNullException.ThrowIfNull(applicationBuilder);

        return applicationBuilder.Use(async (httpContext, next) =>
        {
            var responseHeaders = httpContext.Response.Headers;

            responseHeaders["X-Content-Type-Options"] = "nosniff";
            responseHeaders["X-Frame-Options"] = "DENY";
            responseHeaders["Referrer-Policy"] = "strict-origin-when-cross-origin";
            responseHeaders["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), microphone=(), payment=()";
            responseHeaders["Cross-Origin-Opener-Policy"] = "same-origin";
            responseHeaders["Cross-Origin-Embedder-Policy"] = "require-corp";
            responseHeaders["Cross-Origin-Resource-Policy"] = "same-origin";

            // CSP intentionally strict; APIs do not need to load scripts. The Angular SPA is hosted under
            // a different origin (the gateway) so this CSP applies only to API responses.
            responseHeaders["Content-Security-Policy"] =
                "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";

            // Remove headers that leak implementation detail.
            responseHeaders.Remove("Server");
            responseHeaders.Remove("X-Powered-By");

            await next().ConfigureAwait(false);
        });
    }
}
