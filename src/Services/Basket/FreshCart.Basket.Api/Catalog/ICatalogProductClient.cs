namespace FreshCart.Basket.Api.Catalog;

/// <summary>
/// Read port to the Catalog service used at add-time to verify the product exists and to capture
/// the display snapshot (sku, name, category, price, digital flag) onto the basket line.
/// </summary>
public interface ICatalogProductClient
{
    Task<CatalogProduct?> GetProductAsync(Guid productId, CancellationToken cancellationToken);
}
