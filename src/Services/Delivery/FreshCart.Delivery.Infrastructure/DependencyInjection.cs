using FreshCart.BuildingBlocks.Messaging.MassTransit;
using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Application.Scheduling;
using FreshCart.Delivery.Infrastructure.Geocoding;
using FreshCart.Delivery.Infrastructure.Persistence;
using FreshCart.Delivery.Infrastructure.Persistence.Repositories;
using FreshCart.Delivery.Infrastructure.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace FreshCart.Delivery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDeliveryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        MongoSerializationConfiguration.EnsureRegistered();

        AddMongo(services, configuration);
        AddRepositories(services);
        AddAdapters(services);
        AddMessaging(services, configuration);
        AddStartupTasks(services, environment);

        return services;
    }

    private static void AddMongo(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DeliveryMongoOptions.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string \"{DeliveryMongoOptions.ConnectionStringName}\" is missing.");

        var mongoUrl = MongoUrl.Create(connectionString);
        var options = new DeliveryMongoOptions
        {
            ConnectionString = connectionString,
            DatabaseName = string.IsNullOrWhiteSpace(mongoUrl.DatabaseName)
                ? DeliveryMongoOptions.DefaultDatabaseName
                : mongoUrl.DatabaseName,
        };

        services.AddSingleton(options);
        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoUrl));
        services.AddSingleton<DeliveryMongoContext>();
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IDeliveryRepository, MongoDeliveryRepository>();
        services.AddScoped<ISlotRepository, MongoSlotRepository>();
        services.AddScoped<IZoneRepository, MongoZoneRepository>();
        services.AddScoped<IDriverRepository, MongoDriverRepository>();
        services.AddScoped<IPendingShipmentRepository, MongoPendingShipmentRepository>();
    }

    private static void AddAdapters(IServiceCollection services) =>
        services.AddSingleton<IGeocodingService, DeterministicGeocodingAdapter>();

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration) =>
        services.AddRabbitMqMessageBroker(configuration, typeof(OrderConfirmedConsumer).Assembly);

    private static void AddStartupTasks(IServiceCollection services, IHostEnvironment environment)
    {
        services.AddHostedService<DeliveryMongoIndexInitializer>();

        if (environment.IsDevelopment())
        {
            services.AddHostedService<DevelopmentDataSeeder>();
        }
    }
}
