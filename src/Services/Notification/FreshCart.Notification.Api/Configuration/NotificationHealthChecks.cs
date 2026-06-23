using FreshCart.BuildingBlocks.Messaging.MassTransit;
using MongoDB.Driver;
using RabbitMQ.Client;

namespace FreshCart.Notification.Api.Configuration;

/// <summary>
/// Readiness probes for the three dependencies the service cannot serve without: the MongoDB history
/// store, the Redis SignalR backplane and the RabbitMQ broker the consumers bind to. All are tagged
/// <c>ready</c> so a replica reports unready until each is reachable.
/// </summary>
public static class NotificationHealthChecks
{
    private const string ReadinessTag = "ready";
    private const string MongoHealthCheckName = "notificationsdb";
    private const string CacheHealthCheckName = "cache";
    private const string BrokerHealthCheckName = "rabbitmq";

    public static IServiceCollection AddNotificationHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var cacheConnectionString = configuration.GetConnectionString(NotificationServiceRegistration.CacheConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string \"{NotificationServiceRegistration.CacheConnectionName}\" missing.");

        services.AddHealthChecks()
            .AddMongoDb(
                serviceProvider => serviceProvider.GetRequiredService<IMongoClient>(),
                name: MongoHealthCheckName,
                tags: [ReadinessTag])
            .AddRedis(cacheConnectionString, name: CacheHealthCheckName, tags: [ReadinessTag])
            .AddRabbitMQ(
                _ => CreateBrokerProbeConnectionAsync(configuration),
                name: BrokerHealthCheckName,
                tags: [ReadinessTag]);

        return services;
    }

    private static Task<IConnection> CreateBrokerProbeConnectionAsync(IConfiguration configuration)
    {
        var brokerSection = configuration.GetSection(MessageBrokerOptions.SectionName);
        var brokerHost = brokerSection[nameof(MessageBrokerOptions.Host)]
            ?? throw new InvalidOperationException(
                $"Configuration value \"{MessageBrokerOptions.SectionName}:{nameof(MessageBrokerOptions.Host)}\" is required.");

        var connectionFactory = new ConnectionFactory { Uri = new Uri(brokerHost) };

        var brokerUserName = brokerSection[nameof(MessageBrokerOptions.UserName)];
        if (!string.IsNullOrWhiteSpace(brokerUserName))
        {
            connectionFactory.UserName = brokerUserName;
        }

        var brokerPassword = brokerSection[nameof(MessageBrokerOptions.Password)];
        if (!string.IsNullOrWhiteSpace(brokerPassword))
        {
            connectionFactory.Password = brokerPassword;
        }

        return connectionFactory.CreateConnectionAsync();
    }
}
