using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreshCart.Ordering.Infrastructure.Persistence;

/// <summary>
/// Creates the Ordering schema at startup so a fresh local boot has tables without a manual migration
/// step. EF Core migrations are the production path; EnsureCreated keeps the developer loop
/// frictionless and is only registered in Development.
/// </summary>
public sealed partial class OrderingDatabaseInitializer(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<OrderingDatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        LogSchemaEnsured();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Ordering database schema ensured")]
    private partial void LogSchemaEnsured();
}
