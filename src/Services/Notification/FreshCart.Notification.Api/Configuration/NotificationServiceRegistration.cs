using FreshCart.Notification.Api.Channels;
using FreshCart.Notification.Api.Hubs;
using FreshCart.Notification.Api.Notifications;
using FreshCart.Notification.Api.Notifications.Mongo;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace FreshCart.Notification.Api.Configuration;

/// <summary>
/// Wires the notification fan-out: there is no domain logic here, so the registration is just the
/// store, the channels, the dispatcher and the SignalR backplane that the bus consumers depend on.
/// </summary>
public static class NotificationServiceRegistration
{
    public const string NotificationDatabaseConnectionName = "notificationsdb";
    public const string CacheConnectionName = "cache";

    private const string DefaultDatabaseName = "notificationsdb";

    public static IServiceCollection AddNotificationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddNotificationStore(services, configuration);
        AddFanOut(services);
        AddRealtime(services, configuration);

        return services;
    }

    private static void AddNotificationStore(IServiceCollection services, IConfiguration configuration)
    {
        var notificationConnectionString = configuration.GetConnectionString(NotificationDatabaseConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string \"{NotificationDatabaseConnectionName}\" missing.");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(notificationConnectionString));
        services.AddSingleton(serviceProvider =>
        {
            var databaseName = MongoUrl.Create(notificationConnectionString).DatabaseName ?? DefaultDatabaseName;
            return serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(databaseName);
        });

        services.AddSingleton<MongoNotificationStore>();
        services.AddSingleton<INotificationStore>(serviceProvider =>
            serviceProvider.GetRequiredService<MongoNotificationStore>());

        services.AddHostedService<NotificationIndexInitializer>();
    }

    private static void AddFanOut(IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<INotificationChannel, SignalRNotificationChannel>();
        services.AddSingleton<INotificationChannel, EmailNotificationChannel>();

        services.AddSingleton<NotificationDispatcher>();
        services.AddScoped<NotificationRecorder>();
    }

    private static void AddRealtime(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();

        var cacheConnectionString = configuration.GetConnectionString(CacheConnectionName)
            ?? throw new InvalidOperationException($"Connection string \"{CacheConnectionName}\" missing.");

        services
            .AddSignalR()
            .AddStackExchangeRedis(cacheConnectionString);
    }
}
