using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// Provisions the reporting warehouse on startup so the projection consumers and dashboards have their
/// tables before the first event or query arrives — closing the gap where the warehouse had no schema
/// initializer at all and the service was non-functional live. EF Core creates the invoice and inbox
/// tables it owns first (it also creates the database itself on a fresh server), then the Dapper
/// <see cref="ReportingWarehouseSchema"/> adds the analytical fact and snapshot tables. Both steps are
/// idempotent, so a restart against an existing warehouse re-creates nothing.
/// </summary>
public sealed partial class ReportingWarehouseInitializer(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ReportingWarehouseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using (serviceScope.ConfigureAwait(false))
        {
            // EF Core owns the invoice tables (invoices, invoice_lines, invoice_number_sequences) and the
            // projection inbox; EnsureCreated builds their schema from the model and creates the database
            // on a fresh server. It must run before the Dapper DDL because EnsureCreated initialises the
            // schema only while the database has no tables yet.
            var warehouseDbContext = serviceScope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
            await warehouseDbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

            var connectionFactory = serviceScope.ServiceProvider.GetRequiredService<IWarehouseConnectionFactory>();
            var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using (connection.ConfigureAwait(false))
            {
                await ReportingWarehouseSchema.EnsureCreatedAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }

        LogWarehouseProvisioned();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Reporting warehouse schema provisioned")]
    private partial void LogWarehouseProvisioned();
}
