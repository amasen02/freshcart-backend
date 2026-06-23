using Testcontainers.MongoDb;
using Xunit;

namespace FreshCart.Reviews.Tests.TestInfrastructure;

/// <summary>
/// Spins up a real MongoDB container. The rating summary leans on a server-side group aggregation and
/// the listing paging on server-side sort/skip/limit, which an in-memory list would not reproduce.
/// </summary>
public sealed class MongoFixture : IAsyncLifetime
{
    public const string CollectionName = "Mongo persistence collection";

    private readonly MongoDbContainer _mongoDbContainer = new MongoDbBuilder()
        .WithImage("mongo:8.0")
        .Build();

    public string ConnectionString => _mongoDbContainer.GetConnectionString();

    public Task InitializeAsync() => _mongoDbContainer.StartAsync();

    public Task DisposeAsync() => _mongoDbContainer.DisposeAsync().AsTask();
}
