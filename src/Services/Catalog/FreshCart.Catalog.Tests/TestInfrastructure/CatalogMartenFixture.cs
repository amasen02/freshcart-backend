using FreshCart.Catalog.Api.Models;
using JasperFx;
using Marten;
using Marten.Schema;
using Testcontainers.PostgreSql;
using Xunit;

namespace FreshCart.Catalog.Tests.TestInfrastructure;

/// <summary>
/// Boots a throwaway PostgreSQL container and a Marten document store configured with the same Product
/// schema the service runs against, so the create-product concurrency test exercises the real unique
/// index on Sku — the guard CAT-001 relies on — rather than an in-memory substitute. Shared
/// across the collection to amortise container start-up.
/// </summary>
public sealed class CatalogMartenFixture : IAsyncLifetime
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
            martenOptions.DatabaseSchemaName = "catalog";
            martenOptions.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
            martenOptions.UseSystemTextJsonForSerialization();

            // Mirrors the production Product schema (Catalog.Api composition root): the unique index on
            // Sku is what turns a racing duplicate create into a database-level conflict.
            martenOptions.Schema.For<Product>()
                .UniqueIndex(UniqueIndexType.Computed, product => product.Sku)
                .Index(product => product.Slug);
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
