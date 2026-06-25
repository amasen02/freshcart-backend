using FreshCart.Ordering.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace FreshCart.Ordering.Tests.Persistence;

/// <summary>
/// Boots a throwaway SQL Server container and applies the Ordering schema into a dedicated database, so
/// the EF-Core outbox store runs against the same engine and semantics (UPDATE row locks) it uses in
/// production. A dedicated catalog matters because EnsureCreated skips table creation when the target
/// database already holds tables, which the server's default database does. Shared across the collection.
/// </summary>
public sealed class OutboxIntegrationFixture : IAsyncLifetime
{
    private const string TestDatabaseName = "freshcart_ordering_outbox_tests";

    private readonly MsSqlContainer msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("FreshCart!IntegrationTest1")
        .Build();

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await msSqlContainer.StartAsync();

        ConnectionString = new SqlConnectionStringBuilder(msSqlContainer.GetConnectionString())
        {
            InitialCatalog = TestDatabaseName,
        }.ConnectionString;

        var dbContext = CreateDbContext();
        await using (dbContext.ConfigureAwait(false))
        {
            await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
        }
    }

    public OrderingDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<OrderingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    public async Task DisposeAsync() => await msSqlContainer.DisposeAsync();
}
