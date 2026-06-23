using FreshCart.BuildingBlocks.Exceptions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FreshCart.Identity.Api.Configuration;

/// <summary>
/// Configures the anti-forgery cookie + header pair used to defend against CSRF when the browser
/// presents the FreshCart session cookie. The SPA reads the readable <c>XSRF-TOKEN</c> cookie and
/// echoes it back in the <c>X-XSRF-TOKEN</c> header on every state-changing call.
/// </summary>
public static class AntiforgeryConfiguration
{
    public const string ClientReadableCookieName = "XSRF-TOKEN";

    public const string ClientHeaderName = "X-XSRF-TOKEN";

    public const string AntiforgeryCookieName = "FreshCart.Antiforgery";

    public static IServiceCollection AddFreshCartAntiforgery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAntiforgery(antiforgeryOptions =>
        {
            antiforgeryOptions.HeaderName = ClientHeaderName;
            antiforgeryOptions.Cookie.Name = AntiforgeryCookieName;
            antiforgeryOptions.Cookie.HttpOnly = true;
            antiforgeryOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            antiforgeryOptions.Cookie.SameSite = SameSiteMode.Strict;
        });

        return services;
    }

    /// <summary>
    /// Emits the readable XSRF cookie that the SPA echoes back. Call from a low-risk authenticated
    /// endpoint (for example <c>GET /auth/anti-forgery-token</c>) so the SPA can refresh it lazily.
    /// </summary>
    public static void IssueAntiforgeryCookie(HttpContext httpContext, IAntiforgery antiforgery)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(antiforgery);

        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        if (tokens.RequestToken is null)
        {
            return;
        }

        httpContext.Response.Cookies.Append(
            ClientReadableCookieName,
            tokens.RequestToken,
            new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
            });
    }

    /// <summary>
    /// Validates the anti-forgery token on a state-changing request, but only for browser (cookie)
    /// callers: a request that carries the anti-forgery cookie must echo the matching header. Bearer /
    /// service callers present no ambient cookie and are not CSRF-exposed, so the check is skipped for
    /// them rather than rejecting them. Throws <see cref="BadRequestException"/> on a missing or invalid
    /// token so the response is a 400, not a 500.
    /// </summary>
    public static async Task ValidateBrowserRequestAsync(HttpContext httpContext, IAntiforgery antiforgery)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(antiforgery);

        if (!httpContext.Request.Cookies.ContainsKey(AntiforgeryCookieName))
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(httpContext).ConfigureAwait(false);
        }
        catch (AntiforgeryValidationException exception)
        {
            throw new BadRequestException("Anti-forgery token validation failed.", exception.Message);
        }
    }
}
