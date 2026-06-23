using FreshCart.CustomerSupport.Api.Assignment;
using FreshCart.CustomerSupport.Api.Authentication;
using FreshCart.CustomerSupport.Api.Persistence;
using FreshCart.CustomerSupport.Api.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using StackExchange.Redis;

namespace FreshCart.CustomerSupport.Api;

/// <summary>
/// Composition root for the support service: MongoDB session and transcript stores, the Redis-backed
/// availability registry and queues, the SignalR hub with a Redis backplane, and the conversation
/// coordinator that ties them together.
/// </summary>
public static class DependencyInjection
{
    private const string MongoConnectionStringName = SupportChatMongoContext.ConnectionStringName;
    private const string CacheConnectionName = "cache";
    private const string MongoHealthCheckName = "supportchatdb";
    private const string RedisHealthCheckName = "cache";
    private const string ReadinessHealthCheckTag = "ready";

    public static IServiceCollection AddCustomerSupportServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(TimeProvider.System);

        AddMongoPersistence(services, configuration);
        var redisConnectionString = AddRedisAssignment(services, configuration);
        AddRealtime(services, redisConnectionString);

        return services;
    }

    private static void AddMongoPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var mongoConnectionString = configuration.GetConnectionString(MongoConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string \"{MongoConnectionStringName}\" missing.");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
        services.AddSingleton(serviceProvider =>
        {
            var databaseName = MongoUrl.Create(mongoConnectionString).DatabaseName
                ?? SupportChatMongoContext.DefaultDatabaseName;

            return serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(databaseName);
        });

        services.AddSingleton<SupportChatMongoContext>();
        services.AddSingleton<IChatSessionRepository, MongoChatSessionRepository>();
        services.AddSingleton<IChatMessageRepository, MongoChatMessageRepository>();
        services.AddHostedService<SupportPersistenceInitializer>();

        services.AddHealthChecks()
            .AddMongoDb(
                serviceProvider => serviceProvider.GetRequiredService<IMongoClient>(),
                name: MongoHealthCheckName,
                tags: [ReadinessHealthCheckTag]);
    }

    private static string AddRedisAssignment(IServiceCollection services, IConfiguration configuration)
    {
        var cacheConnectionString = configuration.GetConnectionString(CacheConnectionName)
            ?? throw new InvalidOperationException($"Connection string \"{CacheConnectionName}\" missing.");

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(cacheConnectionString));

        services.AddSingleton<IAgentAvailabilityRegistry, RedisAgentAvailabilityRegistry>();
        services.AddSingleton<IAgentAssignmentStrategy, RedisAgentAssignmentStrategy>();
        services.AddSingleton<IChatWaitingLine, RedisChatWaitingLine>();

        services.AddHealthChecks()
            .AddRedis(cacheConnectionString, name: RedisHealthCheckName, tags: [ReadinessHealthCheckTag]);

        return cacheConnectionString;
    }

    private static void AddRealtime(IServiceCollection services, string redisConnectionString)
    {
        services.AddSingleton<ChatSessionCoordinator>();
        services.AddSingleton<ISupportChatNotifier, SignalRSupportChatNotifier>();
        services.AddSingleton<IUserIdProvider, SupportUserIdProvider>();

        // The Redis backplane lets any replica deliver an event to a connection held by another
        // replica, which is what makes the round-robin assignment correct beyond a single instance.
        services.AddSignalR()
            .AddStackExchangeRedis(redisConnectionString, redisOptions =>
                redisOptions.Configuration.ChannelPrefix = RedisChannel.Literal("freshcart-support"));
    }
}
