using Microsoft.Extensions.Caching.Hybrid;

namespace FreshCart.Basket.Api.Persistence;

public sealed class HybridBasketCacheInvalidator(HybridCache hybridCache) : IBasketCacheInvalidator
{
    public async Task InvalidateAsync(Guid customerId, CancellationToken cancellationToken) =>
        await hybridCache
            .RemoveAsync(BasketCachePolicy.CustomerBasketKey(customerId), cancellationToken)
            .ConfigureAwait(false);
}
