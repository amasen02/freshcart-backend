using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace FreshCart.Gateway.Yarp.Configuration;

/// <summary>
/// Configures the cookie scheme the gateway uses to read the Identity session ticket. The gateway is
/// a pure reader: it never issues the cookie, it only decrypts and authenticates it to learn who the
/// caller is before exchanging the session for a downstream bearer token. The cookie name and the
/// 401/403 suppression events match Identity so the same ticket round-trips.
/// </summary>
public static class GatewayAuthenticationConfiguration
{
    private static readonly TimeSpan SessionAbsoluteLifetime = TimeSpan.FromHours(8);

    public static IServiceCollection AddGatewayCookieAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddAuthentication(authenticationOptions =>
            {
                authenticationOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authenticationOptions.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, ConfigureSessionCookie);

        services.AddAuthorization();

        return services;
    }

    private static void ConfigureSessionCookie(CookieAuthenticationOptions cookieOptions)
    {
        cookieOptions.Cookie.Name = GatewayCookieDefaults.SessionCookieName;
        cookieOptions.Cookie.HttpOnly = true;
        cookieOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        cookieOptions.Cookie.SameSite = SameSiteMode.Strict;
        cookieOptions.Cookie.IsEssential = true;
        cookieOptions.ExpireTimeSpan = SessionAbsoluteLifetime;
        cookieOptions.SlidingExpiration = true;

        cookieOptions.Events.OnRedirectToLogin = redirectContext =>
        {
            redirectContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        cookieOptions.Events.OnRedirectToAccessDenied = redirectContext =>
        {
            redirectContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    }
}
