using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FreshCart.Gateway.Yarp.Configuration;

/// <summary>
/// Small composition helpers kept out of <c>Program.cs</c> so the pipeline reads as a single ordered
/// list. The clock is registered through <see cref="TimeProvider"/> so tests can drive the token
/// cache deterministically.
/// </summary>
public static class GatewayServiceRegistration
{
    private const string CacheConnectionStringName = "cache";
    private const string CacheReadinessCheckName = "cache";
    private const string ReadinessTag = "ready";

    public static IServiceCollection TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        return services;
    }

    public static IServiceCollection AddGatewayHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var cacheConnectionString = configuration.GetConnectionString(CacheConnectionStringName);
        if (string.IsNullOrWhiteSpace(cacheConnectionString))
        {
            return services;
        }

        services.AddHealthChecks()
            .AddRedis(
                redisConnectionString: cacheConnectionString,
                name: CacheReadinessCheckName,
                tags: [ReadinessTag]);

        return services;
    }
}
