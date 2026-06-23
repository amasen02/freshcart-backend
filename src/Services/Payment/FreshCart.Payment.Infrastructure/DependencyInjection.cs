using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Infrastructure.EventStore;
using FreshCart.Payment.Infrastructure.Providers;
using FreshCart.Payment.Infrastructure.ReadModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace FreshCart.Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var eventStoreConnectionString = configuration.GetConnectionString(MongoPaymentEventStore.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string \"{MongoPaymentEventStore.ConnectionStringName}\" missing.");

        var readModelConnectionString = configuration.GetConnectionString(PaymentReadModelSchema.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string \"{PaymentReadModelSchema.ConnectionStringName}\" missing.");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(eventStoreConnectionString));
        services.AddSingleton(serviceProvider =>
        {
            var databaseName = MongoUrl.Create(eventStoreConnectionString).DatabaseName
                ?? MongoPaymentEventStore.DefaultDatabaseName;

            return serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(databaseName);
        });

        services.AddSingleton<MongoPaymentEventStore>();
        services.AddSingleton<IPaymentEventStore>(serviceProvider =>
            serviceProvider.GetRequiredService<MongoPaymentEventStore>());

        services.AddScoped<ISqlConnectionFactory>(_ => new SqlConnectionFactory(readModelConnectionString));
        services.AddScoped<IPaymentReadModelWriter, SqlPaymentReadModelWriter>();
        services.AddScoped<IPaymentReadQueries, DapperPaymentReadQueries>();

        services.AddSingleton<IPaymentProvider, SimulatedCardPaymentProvider>();

        services.AddHostedService<PaymentPersistenceInitializer>();

        return services;
    }
}
