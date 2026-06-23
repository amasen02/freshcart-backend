using Microsoft.Extensions.Caching.Hybrid;

namespace FreshCart.Catalog.Api.Caching;

/// <summary>
/// Single source of truth for catalog cache keys and lifetimes. The in-process L1 copies are kept
/// deliberately shorter than the distributed entries so other instances observe invalidations
/// (which only reach Redis) within a minute or two rather than the full distributed lifetime.
/// </summary>
public static class CatalogCachePolicy
{
    public const string CategoryTreeKey = "catalog:categories:tree";

    private const string ProductKeyPrefix = "catalog:product:";

    public static readonly TimeSpan ProductDistributedExpiry = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan ProductLocalExpiry = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan CategoryTreeDistributedExpiry = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan CategoryTreeLocalExpiry = TimeSpan.FromMinutes(2);

    public static readonly HybridCacheEntryOptions ProductEntryOptions = new()
    {
        Expiration = ProductDistributedExpiry,
        LocalCacheExpiration = ProductLocalExpiry,
    };

    public static readonly HybridCacheEntryOptions CategoryTreeEntryOptions = new()
    {
        Expiration = CategoryTreeDistributedExpiry,
        LocalCacheExpiration = CategoryTreeLocalExpiry,
    };

    public static string ProductKey(Guid productId) => $"{ProductKeyPrefix}{productId}";

    public static string ProductKey(string slug) => $"{ProductKeyPrefix}{slug}";
}
