using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Persistence;

namespace FreshCart.Basket.Tests.Support;

/// <summary>
/// In-memory basket store for handler tests. MutateAsync runs the same load-apply-persist cycle the
/// Marten repository does, so a test asserts the resulting persisted state rather than mock call
/// counts. A single optimistic-concurrency conflict can be armed to prove the mutation re-runs
/// against a fresh snapshot.
/// </summary>
public sealed class InMemoryBasketRepository : IBasketRepository
{
    private ShoppingBasket? _basket;
    private Func<ShoppingBasket?>? _conflictSnapshotFactory;

    public int UpsertCount { get; private set; }

    public int MutateWriteCount { get; private set; }

    public void Seed(ShoppingBasket basket) => _basket = basket;

    /// <summary>
    /// Arms one mutate to observe an intervening write: the first apply sees the current snapshot,
    /// then the store is swapped for <paramref name="winningSnapshot"/> and the mutation re-runs
    /// against it, mirroring a lost optimistic-concurrency race.
    /// </summary>
    public void InjectSingleConflict(ShoppingBasket winningSnapshot) =>
        _conflictSnapshotFactory = () => winningSnapshot;

    public Task<ShoppingBasket?> GetAsync(Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult(_basket);

    public Task UpsertAsync(ShoppingBasket basket, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(basket);

        UpsertCount++;
        _basket = basket;
        return Task.CompletedTask;
    }

    public Task MutateAsync(
        Guid customerId,
        Func<ShoppingBasket?, ShoppingBasket?> mutate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        var firstResult = mutate(_basket);

        if (_conflictSnapshotFactory is not null)
        {
            _basket = _conflictSnapshotFactory();
            _conflictSnapshotFactory = null;
            firstResult = mutate(_basket);
        }

        if (firstResult is not null)
        {
            MutateWriteCount++;
            _basket = firstResult;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid customerId, CancellationToken cancellationToken)
    {
        _basket = null;
        return Task.CompletedTask;
    }

    public Task ArchiveAsync(ArchivedBasket archivedBasket, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
