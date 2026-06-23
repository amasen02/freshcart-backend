namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Eviction port for cached basket entries. Used by writes that bypass the repository decorator:
/// the checkout unit of work deletes the live basket inside one Marten transaction, and the price
/// refresh consumer rewrites documents in bulk; both must evict the stale cache entry afterwards.
/// </summary>
public interface IBasketCacheInvalidator
{
    Task InvalidateAsync(Guid customerId, CancellationToken cancellationToken);
}
