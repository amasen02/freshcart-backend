using FreshCart.Inventory.Api.Persistence;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace FreshCart.Inventory.Tests.Common;

/// <summary>
/// Spins up a real SQL Server container via Testcontainers and applies the exact schema the service
/// runs against. The reservation logic depends on SQL Server semantics (UPDLOCK row locks, check
/// constraints, unique-index race behaviour) that no in-memory substitute reproduces.
/// </summary>
public sealed class InventoryDatabaseFixture : IAsyncLifetime
{
    public const string CollectionName = "Inventory database collection";

    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("FreshCart!IntegrationTest1")
        .Build();

    public string ConnectionString => _msSqlContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync().ConfigureAwait(false);

        var schemaConnection = new SqlConnection(ConnectionString);
        await using (schemaConnection.ConfigureAwait(false))
        {
            await schemaConnection.OpenAsync().ConfigureAwait(false);
            await InventorySchema.EnsureCreatedAsync(schemaConnection, CancellationToken.None).ConfigureAwait(false);
        }
    }

    public Task DisposeAsync() => _msSqlContainer.DisposeAsync().AsTask();
}
