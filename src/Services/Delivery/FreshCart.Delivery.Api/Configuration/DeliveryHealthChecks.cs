using FreshCart.BuildingBlocks.Messaging.MassTransit;
using FreshCart.Delivery.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RabbitMQ.Client;

namespace FreshCart.Delivery.Api.Configuration;

/// <summary>
/// Registers the readiness probes for the delivery host's real dependencies. The MongoDB check reuses
/// the same client the service uses; the RabbitMQ check opens a short-lived connection to the broker
/// described by the <c>MessageBroker</c> configuration section. Both are tagged <c>ready</c> so the
/// <c>/ready</c> endpoint reflects genuine dependency state.
/// </summary>
internal static class DeliveryHealthChecks
{
    private const string ReadinessTag = "ready";

    public static IServiceCollection AddDeliveryHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var brokerSection = configuration.GetSection(MessageBrokerOptions.SectionName);
        var brokerHost = brokerSection[nameof(MessageBrokerOptions.Host)]
            ?? throw new InvalidOperationException(
                $"Configuration value \"{MessageBrokerOptions.SectionName}:{nameof(MessageBrokerOptions.Host)}\" is required.");
        var brokerUserName = brokerSection[nameof(MessageBrokerOptions.UserName)];
        var brokerPassword = brokerSection[nameof(MessageBrokerOptions.Password)];

        services
            .AddHealthChecks()
            .AddMongoDb(
                clientFactory: serviceProvider => serviceProvider.GetRequiredService<IMongoClient>(),
                databaseNameFactory: serviceProvider =>
                    serviceProvider.GetRequiredService<DeliveryMongoOptions>().DatabaseName,
                name: "deliverydb",
                failureStatus: null,
                tags: [ReadinessTag])
            .AddRabbitMQ(
                async _ =>
                {
                    var connectionFactory = new ConnectionFactory
                    {
                        Uri = new Uri(brokerHost),
                        UserName = brokerUserName ?? ConnectionFactory.DefaultUser,
                        Password = brokerPassword ?? ConnectionFactory.DefaultPass,
                    };

                    return await connectionFactory.CreateConnectionAsync().ConfigureAwait(false);
                },
                name: "rabbitmq",
                failureStatus: null,
                tags: [ReadinessTag]);

        return services;
    }
}
