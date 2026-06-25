using System.Globalization;
using DotNet.Testcontainers.Containers;
using FreshCart.Delivery.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace FreshCart.Delivery.Tests.Infrastructure;

/// <summary>
/// Boots a throwaway single-node MongoDB replica set for the repository, outbox and transaction tests,
/// and ensures the production index set (including the 2dsphere zone index) exists before any test runs,
/// so the geo-match tests exercise the same query path the service uses. A replica set is required
/// because the delivery write and its outbox message commit in one transaction. Shared across the
/// collection to amortise container start-up.
/// </summary>
public sealed class MongoIntegrationFixture : IAsyncLifetime
{
    private const string DatabaseName = "delivery-tests";

    private readonly IContainer mongoContainer = MongoReplicaSetContainer.Build();

    public IMongoClient Client { get; private set; } = null!;

    public string ConnectionString { get; private set; } = null!;

    public DeliveryMongoContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        MongoSerializationConfiguration.EnsureRegistered();

        await mongoContainer.StartAsync();
        ConnectionString = await MongoReplicaSetContainer.InitiateAsync(mongoContainer);

        Client = new MongoClient(ConnectionString);
        Context = new DeliveryMongoContext(Client, new DeliveryMongoOptions
        {
            ConnectionString = ConnectionString,
            DatabaseName = DatabaseName,
        });

        var indexInitializer = new DeliveryMongoIndexInitializer(
            Context,
            NullLogger<DeliveryMongoIndexInitializer>.Instance);
        await indexInitializer.StartAsync(CancellationToken.None);
    }

    /// <summary>
    /// A context over a fresh database on the same replica set, so a test that exercises the outbox or a
    /// unique-index conflict cannot observe another test's documents.
    /// </summary>
    public DeliveryMongoContext CreateIsolatedContext() => new(
        Client,
        new DeliveryMongoOptions
        {
            ConnectionString = ConnectionString,
            DatabaseName = "delivery_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
        });

    public async Task DisposeAsync()
    {
        (Client as IDisposable)?.Dispose();
        await mongoContainer.DisposeAsync();
    }
}
