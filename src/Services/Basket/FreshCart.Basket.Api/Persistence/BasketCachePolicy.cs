using Microsoft.Extensions.Caching.Hybrid;

namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Single source of truth for basket cache keys and lifetimes. The distributed entry lives for
/// 30 minutes; the in-process L1 copy is kept deliberately shorter so other instances observe
/// invalidations (which only reach Redis) within minutes rather than half an hour.
/// </summary>
public static class BasketCachePolicy
{
    private const string CustomerBasketKeyPrefix = "basket:";

    public static readonly TimeSpan DistributedExpiry = TimeSpan.FromMinutes(30);

    public static readonly TimeSpan LocalExpiry = TimeSpan.FromMinutes(5);

    public static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        Expiration = DistributedExpiry,
        LocalCacheExpiration = LocalExpiry,
    };

    public static string CustomerBasketKey(Guid customerId) => $"{CustomerBasketKeyPrefix}{customerId}";
}
