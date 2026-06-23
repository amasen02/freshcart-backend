using FreshCart.Inventory.Api.Repositories;

namespace FreshCart.Inventory.Api.Persistence;

public sealed partial class InventorySchemaInitializer(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<InventorySchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using (serviceScope.ConfigureAwait(false))
        {
            var connectionFactory = serviceScope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
            var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            await InventorySchema.EnsureCreatedAsync(connection, cancellationToken).ConfigureAwait(false);
        }

        LogSchemaVerified();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Inventory schema verified")]
    private partial void LogSchemaVerified();
}
