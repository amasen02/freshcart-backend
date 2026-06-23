using Microsoft.Extensions.Caching.Hybrid;

namespace FreshCart.Catalog.Tests.TestInfrastructure;

/// <summary>
/// HybridCache stand-in that always misses: every read invokes the factory and records the
/// requested key, so handler tests can assert both the loaded data and the cache key contract.
/// </summary>
internal sealed class PassThroughHybridCache : HybridCache
{
    public List<string> RequestedKeys { get; } = [];

    public List<string> RemovedKeys { get; } = [];

    public override ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        RequestedKeys.Add(key);
        return factory(state, cancellationToken);
    }

    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        RemovedKeys.Add(key);
        return ValueTask.CompletedTask;
    }

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}
