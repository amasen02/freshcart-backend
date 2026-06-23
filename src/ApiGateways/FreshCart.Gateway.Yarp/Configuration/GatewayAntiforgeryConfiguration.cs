using Microsoft.AspNetCore.Http;

namespace FreshCart.Gateway.Yarp.Configuration;

/// <summary>
/// Configures the anti-forgery service so the gateway validates the double-submit XSRF token using
/// the same cookie name and header the Identity service issued. Sharing the data-protection ring lets
/// the gateway validate a token an Identity replica created.
/// </summary>
public static class GatewayAntiforgeryConfiguration
{
    public static IServiceCollection AddGatewayAntiforgery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAntiforgery(antiforgeryOptions =>
        {
            antiforgeryOptions.HeaderName = GatewayCookieDefaults.AntiforgeryHeaderName;
            antiforgeryOptions.Cookie.Name = GatewayCookieDefaults.AntiforgeryCookieName;
            antiforgeryOptions.Cookie.HttpOnly = true;
            antiforgeryOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            antiforgeryOptions.Cookie.SameSite = SameSiteMode.Strict;
        });

        return services;
    }
}
