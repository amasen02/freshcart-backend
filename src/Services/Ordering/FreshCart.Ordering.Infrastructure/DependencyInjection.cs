using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Infrastructure.Inventory;
using FreshCart.Ordering.Infrastructure.Payment;
using FreshCart.Ordering.Infrastructure.Persistence;
using FreshCart.Ordering.Infrastructure.Persistence.Interceptors;
using FreshCart.Ordering.Infrastructure.Persistence.Reads;
using FreshCart.Ordering.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FreshCart.Ordering.Infrastructure;

/// <summary>
/// Composition root for the Ordering infrastructure: EF Core write side with the outbox interceptor,
/// the Dapper read side, the Inventory gRPC adapter and the Payment HTTP adapter. MassTransit is
/// wired in the API host because the EF saga repository must be configured inside AddMassTransit.
/// </summary>
public static class DependencyInjection
{
    public const string OrderingConnectionName = "orderingdb";

    private const string PaymentBaseAddressConfigurationKey = "Services:Payment:BaseAddress";

    public static IServiceCollection AddOrderingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IServiceTokenProvider, ServiceTokenProvider>();

        AddWriteSide(services, configuration);
        AddReadSide(services, configuration);
        AddInventoryClient(services);
        AddPaymentClient(services, configuration);

        return services;
    }

    private static void AddWriteSide(IServiceCollection services, IConfiguration configuration)
    {
        var orderingConnectionString = ResolveConnectionString(configuration);

        services.AddSingleton<DomainEventsToOutboxInterceptor>();

        services.AddDbContext<OrderingDbContext>((serviceProvider, dbContextOptions) =>
            dbContextOptions
                .UseSqlServer(
                    orderingConnectionString,
                    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(maxRetryCount: 5))
                .AddInterceptors(serviceProvider.GetRequiredService<DomainEventsToOutboxInterceptor>()));

        services.AddScoped<IOrderRepository, EntityFrameworkOrderRepository>();
        services.AddScoped<IOutboxStore, EntityFrameworkOutboxStore>();
        services.Configure<OutboxPublisherOptions>(configuration.GetSection(OutboxPublisherOptions.SectionName));
    }

    private static void AddReadSide(IServiceCollection services, IConfiguration configuration)
    {
        var orderingConnectionString = ResolveConnectionString(configuration);

        services.AddSingleton(new OrderingConnectionOptions { ConnectionString = orderingConnectionString });
        services.AddScoped<IOrderingConnectionFactory, SqlServerOrderingConnectionFactory>();
        services.AddScoped<IOrderReadQueries, DapperOrderReadQueries>();
    }

    private static void AddInventoryClient(IServiceCollection services)
    {
        services.AddSingleton<InventoryGrpcChannelFactory>();
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<InventoryGrpcChannelFactory>().CreateClient());
        services.AddScoped<IInventoryClient, GrpcInventoryClient>();
    }

    private static void AddPaymentClient(IServiceCollection services, IConfiguration configuration)
    {
        var paymentBaseAddress = configuration[PaymentBaseAddressConfigurationKey]
            ?? throw new InvalidOperationException(
                $"Configuration value \"{PaymentBaseAddressConfigurationKey}\" is required.");

        services.AddTransient<ServiceAuthenticationHandler>();
        services.AddHttpClient<IPaymentClient, HttpPaymentClient>(httpClient =>
                httpClient.BaseAddress = new Uri(paymentBaseAddress))
            .AddHttpMessageHandler<ServiceAuthenticationHandler>();
    }

    private static string ResolveConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString(OrderingConnectionName)
            ?? throw new InvalidOperationException($"Connection string \"{OrderingConnectionName}\" is missing.");
}
