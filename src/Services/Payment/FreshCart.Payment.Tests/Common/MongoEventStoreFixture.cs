using Testcontainers.MongoDb;
using Xunit;

namespace FreshCart.Payment.Tests.Common;

/// <summary>
/// Spins up a real MongoDB container via Testcontainers. The optimistic-concurrency behaviour of
/// the event store rides on the server-side unique index, which no in-memory substitute reproduces.
/// </summary>
public sealed class MongoEventStoreFixture : IAsyncLifetime
{
    public const string CollectionName = "Payment event store collection";

    private readonly MongoDbContainer _mongoDbContainer = new MongoDbBuilder()
        .WithImage("mongo:8.0")
        .Build();

    public string ConnectionString => _mongoDbContainer.GetConnectionString();

    public Task InitializeAsync() => _mongoDbContainer.StartAsync();

    public Task DisposeAsync() => _mongoDbContainer.DisposeAsync().AsTask();
}
