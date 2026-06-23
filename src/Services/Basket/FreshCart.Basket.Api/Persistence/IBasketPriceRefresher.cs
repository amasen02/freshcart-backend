namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Bulk-update port used when Catalog announces a price change: rewrites the stored unit price on
/// every live basket line holding the product and reports which customers were touched so their
/// cache entries can be evicted.
/// </summary>
public interface IBasketPriceRefresher
{
    Task<IReadOnlyList<Guid>> RefreshUnitPriceAsync(Guid productId, decimal newUnitPrice, CancellationToken cancellationToken);
}
