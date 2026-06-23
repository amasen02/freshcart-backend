using FreshCart.Delivery.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace FreshCart.Delivery.Tests.Infrastructure;

/// <summary>
/// Boots a throwaway MongoDB container for the repository tests and ensures the production index set
/// (including the 2dsphere zone index) exists before any test runs, so the geo-match tests exercise the
/// same query path the service uses. Shared across the collection to amortise container start-up.
/// </summary>
public sealed class MongoIntegrationFixture : IAsyncLifetime
{
    private const string DatabaseName = "delivery-tests";

    private readonly MongoDbContainer mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .Build();

    public DeliveryMongoContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        MongoSerializationConfiguration.EnsureRegistered();

        await mongoContainer.StartAsync();

        var mongoClient = new MongoClient(mongoContainer.GetConnectionString());
        var options = new DeliveryMongoOptions
        {
            ConnectionString = mongoContainer.GetConnectionString(),
            DatabaseName = DatabaseName,
        };

        Context = new DeliveryMongoContext(mongoClient, options);

        var indexInitializer = new DeliveryMongoIndexInitializer(
            Context,
            NullLogger<DeliveryMongoIndexInitializer>.Instance);
        await indexInitializer.StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync() => await mongoContainer.DisposeAsync();
}
