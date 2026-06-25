using Marten;
using Testcontainers.PostgreSql;
using Xunit;

namespace FreshCart.Basket.Tests.Persistence;

/// <summary>
/// Boots a throwaway PostgreSQL container and a Marten document store, so the Marten-backed outbox store
/// runs the claim against the same engine (and FOR UPDATE / row-lock semantics) it uses in production.
/// Shared across the collection to amortise container start-up.
/// </summary>
public sealed class OutboxIntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    public IDocumentStore DocumentStore { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await postgreSqlContainer.StartAsync();

        DocumentStore = Marten.DocumentStore.For(martenOptions =>
        {
            martenOptions.Connection(postgreSqlContainer.GetConnectionString());
            martenOptions.DatabaseSchemaName = "basket";
        });
    }

    public async Task DisposeAsync()
    {
        if (DocumentStore is not null)
        {
            await DocumentStore.DisposeAsync();
        }

        await postgreSqlContainer.DisposeAsync();
    }
}
