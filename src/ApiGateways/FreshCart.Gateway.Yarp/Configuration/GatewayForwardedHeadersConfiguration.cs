using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace FreshCart.Gateway.Yarp.Configuration;

/// <summary>
/// Trusted-hop configuration for <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c>. Only the proxies named
/// in <c>ForwardedHeaders:KnownProxies</c> and the networks in <c>ForwardedHeaders:KnownNetworks</c> may
/// rewrite the connection address.
/// <para>
/// This matters because the rate limiter partitions on <c>Connection.RemoteIpAddress</c>. ASP.NET Core's
/// forwarded-headers middleware skips its origin check entirely when both trusted lists are empty, so an
/// unconfigured gateway would rewrite the connection address from a header any caller can set, handing every
/// request a fresh rate-limit partition and defeating the sign-in throttle. When nothing is trusted the
/// middleware is therefore left out of the pipeline altogether rather than run wide open.
/// </para>
/// </summary>
public static class GatewayForwardedHeadersConfiguration
{
    public const string SectionName = "ForwardedHeaders";

    private const string KnownProxiesKey = "KnownProxies";
    private const string KnownNetworksKey = "KnownNetworks";

    public static IServiceCollection AddGatewayForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var forwardedHeadersSection = configuration.GetSection(SectionName);
        var knownProxies = forwardedHeadersSection.GetSection(KnownProxiesKey).Get<string[]>() ?? [];
        var knownNetworks = forwardedHeadersSection.GetSection(KnownNetworksKey).Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(forwardedHeadersOptions =>
        {
            forwardedHeadersOptions.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Exactly one hop is trusted: the ingress controller that terminates TLS. Anything further left in
            // the header was appended by a caller and must not become the partition key.
            forwardedHeadersOptions.ForwardLimit = 1;

            forwardedHeadersOptions.KnownIPNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();

            foreach (var knownProxy in knownProxies)
            {
                forwardedHeadersOptions.KnownProxies.Add(System.Net.IPAddress.Parse(knownProxy));
            }

            foreach (var knownNetwork in knownNetworks)
            {
                forwardedHeadersOptions.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(knownNetwork));
            }
        });

        return services;
    }

    /// <summary>
    /// Adds the forwarded-headers middleware only when a trusted hop is configured. With no trusted hop the
    /// header is ignored and the limiter keeps partitioning on the real socket address.
    /// </summary>
    public static WebApplication UseGatewayForwardedHeaders(this WebApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        var forwardedHeadersOptions = application.Services
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;

        if (forwardedHeadersOptions.KnownProxies.Count == 0 && forwardedHeadersOptions.KnownIPNetworks.Count == 0)
        {
            return application;
        }

        application.UseForwardedHeaders();

        return application;
    }
}
