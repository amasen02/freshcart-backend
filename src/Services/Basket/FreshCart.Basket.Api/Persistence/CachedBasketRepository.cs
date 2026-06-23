using FreshCart.Basket.Api.Domain;
using Microsoft.Extensions.Caching.Hybrid;

namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Decorator that serves basket reads from <see cref="HybridCache"/> (Redis L2 + in-process L1)
/// and invalidates the cached entry on every write. Archive operations pass straight through:
/// archives are keyed by order id and are never read on the hot path.
/// </summary>
public sealed class CachedBasketRepository(IBasketRepository innerRepository, HybridCache hybridCache) : IBasketRepository
{
    public async Task<ShoppingBasket?> GetAsync(Guid customerId, CancellationToken cancellationToken) =>
        await hybridCache.GetOrCreateAsync(
            BasketCachePolicy.CustomerBasketKey(customerId),
            (Repository: innerRepository, CustomerId: customerId),
            static async (state, factoryCancellationToken) =>
                await state.Repository.GetAsync(state.CustomerId, factoryCancellationToken).ConfigureAwait(false),
            BasketCachePolicy.EntryOptions,
            cancellationToken: cancellationToken).ConfigureAwait(false);

    public async Task UpsertAsync(ShoppingBasket basket, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(basket);

        await innerRepository.UpsertAsync(basket, cancellationToken).ConfigureAwait(false);
        await hybridCache.RemoveAsync(BasketCachePolicy.CustomerBasketKey(basket.Id), cancellationToken).ConfigureAwait(false);
    }

    public async Task MutateAsync(
        Guid customerId,
        Func<ShoppingBasket?, ShoppingBasket?> mutate,
        CancellationToken cancellationToken)
    {
        // The mutate-and-retry loop reads the authoritative document straight from the inner store,
        // never the cache, so a stale cached snapshot can never make a retry diverge. The cached entry
        // is evicted afterwards so the next read repopulates from the committed state.
        await innerRepository.MutateAsync(customerId, mutate, cancellationToken).ConfigureAwait(false);
        await hybridCache.RemoveAsync(BasketCachePolicy.CustomerBasketKey(customerId), cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid customerId, CancellationToken cancellationToken)
    {
        await innerRepository.DeleteAsync(customerId, cancellationToken).ConfigureAwait(false);
        await hybridCache.RemoveAsync(BasketCachePolicy.CustomerBasketKey(customerId), cancellationToken).ConfigureAwait(false);
    }

    public Task ArchiveAsync(ArchivedBasket archivedBasket, CancellationToken cancellationToken) =>
        innerRepository.ArchiveAsync(archivedBasket, cancellationToken);
}
