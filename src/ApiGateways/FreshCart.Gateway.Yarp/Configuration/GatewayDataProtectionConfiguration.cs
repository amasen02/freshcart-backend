using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace FreshCart.Gateway.Yarp.Configuration;

/// <summary>
/// Shares the data-protection key ring with the Identity service. The application name and Redis key
/// are identical on both sides; without that the gateway cannot decrypt the <c>FreshCart.Session</c>
/// cookie ticket nor validate an Identity-issued anti-forgery token. When no key-ring connection is
/// configured (integration tests) the host falls back to the in-process key ring.
/// <para>
/// The ring is persisted as plaintext XML, so whoever can read the store can mint a session cookie for
/// any subject and any role. It therefore gets its own connection string, <c>dataprotection</c>, pointing
/// at a store only the gateway and Identity can reach - never the <c>cache</c> instance that Basket,
/// Catalog, CustomerSupport and Notification also hold credentials for. Falling back to the shared cache
/// is allowed in Development only, and refused everywhere else.
/// </para>
/// </summary>
public static class GatewayDataProtectionConfiguration
{
    private const string CacheConnectionStringName = "cache";
    private const string KeyRingConnectionStringName = "dataprotection";

    public static async Task AddGatewaySharedDataProtectionAsync(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var keyRingConnectionString = ResolveKeyRingConnectionString(configuration, environment);
        if (string.IsNullOrWhiteSpace(keyRingConnectionString))
        {
            return;
        }

        var redisConnectionMultiplexer = await ConnectionMultiplexer
            .ConnectAsync(keyRingConnectionString)
            .ConfigureAwait(false);

        // Published for the key ring only. The gateway has no other Redis consumer, and cache traffic must
        // not be pointed at the key-ring store or the isolation above is undone.
        services.AddSingleton<IConnectionMultiplexer>(redisConnectionMultiplexer);
        services
            .AddDataProtection()
            .SetApplicationName(GatewayCookieDefaults.DataProtectionApplicationName)
            .PersistKeysToStackExchangeRedis(
                redisConnectionMultiplexer,
                GatewayCookieDefaults.DataProtectionKeysRedisKey);
    }

    private static string? ResolveKeyRingConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        var keyRingConnectionString = configuration.GetConnectionString(KeyRingConnectionStringName);
        if (!string.IsNullOrWhiteSpace(keyRingConnectionString))
        {
            return keyRingConnectionString;
        }

        var cacheConnectionString = configuration.GetConnectionString(CacheConnectionStringName);
        if (string.IsNullOrWhiteSpace(cacheConnectionString))
        {
            return null;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "ConnectionStrings:dataprotection is not configured. The data-protection key ring is the " +
                "signing and encryption material behind the FreshCart.Session cookie and the anti-forgery " +
                "token, so it must not be written to the application cache that other services can read. " +
                "Point 'dataprotection' at a store reachable only by the gateway and Identity.");
        }

        return cacheConnectionString;
    }
}
