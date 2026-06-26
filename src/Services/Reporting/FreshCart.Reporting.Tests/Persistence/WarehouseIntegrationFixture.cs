using Dapper;
using FreshCart.Reporting.Infrastructure.Persistence.Warehouse;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Testcontainers.MySql;

namespace FreshCart.Reporting.Tests.Persistence;

/// <summary>
/// Boots a throwaway MySQL container for the warehouse projection-writer tests and creates the tables
/// the writer touches. The DDL mirrors the columns the writer and dashboard readers expect; production
/// schema provisioning is a separate, larger gap (no initializer exists for the warehouse — see the
/// REP-SCHEMA finding), so the schema lives here as test infrastructure. Shared across the collection
/// to amortise container start-up.
/// </summary>
public sealed class WarehouseIntegrationFixture : IAsyncLifetime
{
    private readonly MySqlContainer mySqlContainer = new MySqlBuilder()
        .WithImage("mysql:8.0")
        .WithDatabase("reportingdb")
        .Build();

    public IWarehouseConnectionFactory ConnectionFactory { get; private set; } = null!;

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await mySqlContainer.StartAsync();

        ConnectionString = mySqlContainer.GetConnectionString();
        ConnectionFactory = new MySqlWarehouseConnectionFactory(
            new WarehouseConnectionOptions { ConnectionString = ConnectionString });

        await CreateWarehouseTablesAsync();
    }

    // Invoice-number allocation runs entirely through the Dapper connection factory, so the repository's
    // DbContext dependency is never touched on that path. A provider-less context satisfies the constructor
    // without dragging the Pomelo EF provider into the test assembly (whose transitive EF Core version does
    // not match the one Pomelo binds against at runtime).
    public static WarehouseDbContext CreateWarehouseDbContext()
        => new(new DbContextOptionsBuilder<WarehouseDbContext>().Options);

    public async Task DisposeAsync() => await mySqlContainer.DisposeAsync();

    private async Task CreateWarehouseTablesAsync()
    {
        // EF Core owns these two tables in production (WarehouseDbContext.EnsureCreatedAsync), but the
        // Pomelo provider cannot run inside the test assembly, so they are created here with the same shape.
        // The analytical fact and snapshot tables come from the production ReportingWarehouseSchema, so the
        // tests exercise the exact DDL the live initializer applies.
        const string createEfOwnedTablesSql = """
            CREATE TABLE IF NOT EXISTS projection_inbox (
                EventId        CHAR(36)     NOT NULL PRIMARY KEY,
                ProcessedOnUtc DATETIME(6)  NOT NULL
            );

            CREATE TABLE IF NOT EXISTS invoice_number_sequences (
                Year         INT    NOT NULL,
                Kind         INT    NOT NULL,
                LastSequence BIGINT NOT NULL,
                PRIMARY KEY (Year, Kind)
            );
            """;

        var connection = new MySqlConnection(ConnectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.ExecuteAsync(createEfOwnedTablesSql).ConfigureAwait(false);
            await ReportingWarehouseSchema.EnsureCreatedAsync(connection, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
