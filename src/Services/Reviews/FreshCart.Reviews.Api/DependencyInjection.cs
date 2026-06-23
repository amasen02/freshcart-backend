using System.Reflection;
using FluentValidation;
using FreshCart.BuildingBlocks.Behaviors;
using FreshCart.BuildingBlocks.Messaging.MassTransit;
using FreshCart.Reviews.Api.Consumers;
using FreshCart.Reviews.Api.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace FreshCart.Reviews.Api;

/// <summary>
/// Composition root for the reviews service: the MediatR slice pipeline, MongoDB document persistence
/// with its index initializer, and MassTransit wired to consume OrderConfirmed into local purchase
/// entitlements.
/// </summary>
public static class DependencyInjection
{
    private const string MongoConnectionStringName = ReviewsMongoContext.ConnectionStringName;
    private const string MongoHealthCheckName = "reviewsdb";
    private const string MessageBrokerHealthCheckName = "rabbitmq";
    private const string ReadinessHealthCheckTag = "ready";

    public static IServiceCollection AddReviewsServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var apiAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(mediatrConfiguration =>
        {
            mediatrConfiguration.RegisterServicesFromAssembly(apiAssembly);
            mediatrConfiguration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            mediatrConfiguration.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(apiAssembly);
        services.TryAddSingleton(TimeProvider.System);

        AddMongoPersistence(services, configuration);
        AddReviewsMessaging(services, configuration);

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
                ?? ReviewsMongoContext.DefaultDatabaseName;

            return serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(databaseName);
        });

        services.AddSingleton<ReviewsMongoContext>();
        services.AddSingleton<IReviewRepository, MongoReviewRepository>();
        services.AddSingleton<IPurchaseRecordRepository, MongoPurchaseRecordRepository>();
        services.AddHostedService<ReviewsPersistenceInitializer>();

        services.AddHealthChecks()
            .AddMongoDb(
                serviceProvider => serviceProvider.GetRequiredService<IMongoClient>(),
                name: MongoHealthCheckName,
                tags: [ReadinessHealthCheckTag]);
    }

    private static void AddReviewsMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRabbitMqMessageBroker(configuration, typeof(OrderConfirmedConsumer).Assembly);

        services.AddHealthChecks()
            .AddRabbitMQ(
                _ => CreateBrokerProbeConnectionAsync(configuration),
                name: MessageBrokerHealthCheckName,
                tags: [ReadinessHealthCheckTag]);
    }

    private static Task<RabbitMQ.Client.IConnection> CreateBrokerProbeConnectionAsync(IConfiguration configuration)
    {
        var brokerSection = configuration.GetSection(MessageBrokerOptions.SectionName);
        var brokerHost = brokerSection[nameof(MessageBrokerOptions.Host)]
            ?? throw new InvalidOperationException(
                $"Configuration value \"{MessageBrokerOptions.SectionName}:{nameof(MessageBrokerOptions.Host)}\" is required.");

        var connectionFactory = new RabbitMQ.Client.ConnectionFactory { Uri = new Uri(brokerHost) };

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
