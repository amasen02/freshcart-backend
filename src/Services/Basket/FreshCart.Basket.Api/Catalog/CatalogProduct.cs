namespace FreshCart.Basket.Api.Catalog;

public sealed record CatalogProduct(
    Guid ProductId,
    string Sku,
    string Name,
    string PrimaryCategory,
    decimal Price,
    string? ImageUrl,
    bool IsDigital,
    bool IsActive);
