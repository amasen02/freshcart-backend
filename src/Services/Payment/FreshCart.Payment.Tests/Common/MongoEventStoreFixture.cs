using DotNet.Testcontainers.Containers;
using Xunit;

namespace FreshCart.Payment.Tests.Common;

/// <summary>
/// Spins up a real single-node MongoDB replica set via Testcontainers. The optimistic-concurrency
/// behaviour of the event store rides on the server-side unique index, which no in-memory substitute
/// reproduces; the atomic event-plus-projection-marker append additionally requires a replica set, since
/// a standalone mongod rejects multi-document transactions.
/// </summary>
public sealed class MongoEventStoreFixture : IAsyncLifetime
{
    public const string CollectionName = "Payment event store collection";

    private readonly IContainer _mongoContainer = MongoReplicaSetContainer.Build();

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();
        ConnectionString = await MongoReplicaSetContainer.InitiateAsync(_mongoContainer);
    }

    public Task DisposeAsync() => _mongoContainer.DisposeAsync().AsTask();
}
