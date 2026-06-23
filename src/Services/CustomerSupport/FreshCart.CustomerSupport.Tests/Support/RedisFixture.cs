using Testcontainers.Redis;
using Xunit;

namespace FreshCart.CustomerSupport.Tests.Support;

/// <summary>
/// Spins up a real Redis container. The assignment tie-break and the floor-at-zero release are
/// implemented as Lua server-side scripts, so the unit under test is Redis itself; an in-memory
/// fake would not exercise the atomicity that is the whole point of the design.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    public const string CollectionName = "Redis assignment collection";

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7.4")
        .Build();

    public string ConnectionString => _redisContainer.GetConnectionString();

    public Task InitializeAsync() => _redisContainer.StartAsync();

    public Task DisposeAsync() => _redisContainer.DisposeAsync().AsTask();
}
