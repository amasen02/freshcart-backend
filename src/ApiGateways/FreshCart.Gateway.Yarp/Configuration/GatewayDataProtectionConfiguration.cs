using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace FreshCart.Gateway.Yarp.Configuration;

/// <summary>
/// Shares the data-protection key ring with the Identity service. The application name and Redis key
/// are identical on both sides; without that the gateway cannot decrypt the <c>FreshCart.Session</c>
/// cookie ticket nor validate an Identity-issued anti-forgery token. When no cache connection is
/// configured (integration tests) the host falls back to the in-process key ring.
/// </summary>
public static class GatewayDataProtectionConfiguration
{
    private const string CacheConnectionStringName = "cache";

    public static async Task AddGatewaySharedDataProtectionAsync(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var cacheConnectionString = configuration.GetConnectionString(CacheConnectionStringName);
        if (string.IsNullOrWhiteSpace(cacheConnectionString))
        {
            return;
        }

        var redisConnectionMultiplexer = await ConnectionMultiplexer
            .ConnectAsync(cacheConnectionString)
            .ConfigureAwait(false);

        services.AddSingleton<IConnectionMultiplexer>(redisConnectionMultiplexer);
        services
            .AddDataProtection()
            .SetApplicationName(GatewayCookieDefaults.DataProtectionApplicationName)
            .PersistKeysToStackExchangeRedis(
                redisConnectionMultiplexer,
                GatewayCookieDefaults.DataProtectionKeysRedisKey);
    }
}
