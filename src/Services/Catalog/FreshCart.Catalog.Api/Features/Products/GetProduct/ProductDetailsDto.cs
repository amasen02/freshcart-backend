using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Api.Features.Products.GetProduct;

/// <summary>
/// Full product read model. <c>PrimaryCategory</c>, <c>Price</c>, <c>ImageUrl</c>, <c>IsDigital</c>
/// and <c>IsActive</c> are part of the cross-service contract consumed by Basket's product client;
/// inactive products are returned with the flag down instead of a 404 so consumers can decide.
/// </summary>
public sealed record ProductDetailsDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string Sku,
    decimal Price,
    string CurrencyCode,
    Guid CategoryId,
    string PrimaryCategory,
    Guid BrandId,
    string BrandName,
    bool IsActive,
    bool IsDigital,
    string? ImageUrl,
    IReadOnlyList<ProductImage> Images,
    IReadOnlyList<ProductAttribute> Attributes,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset UpdatedOnUtc)
{
    public static ProductDetailsDto FromProduct(Product product, string primaryCategory, string brandName)
    {
        ArgumentNullException.ThrowIfNull(product);

        var primaryImageUrl = product.Images.FirstOrDefault(image => image.IsPrimary)?.Url
            ?? product.Images.FirstOrDefault()?.Url;

        return new ProductDetailsDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.Sku,
            product.BasePrice,
            product.CurrencyCode,
            product.CategoryId,
            primaryCategory,
            product.BrandId,
            brandName,
            product.IsActive,
            product.IsDigital,
            primaryImageUrl,
            [.. product.Images],
            [.. product.Attributes],
            product.CreatedOnUtc,
            product.UpdatedOnUtc);
    }
}
