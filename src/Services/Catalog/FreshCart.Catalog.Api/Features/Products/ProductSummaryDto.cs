using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Api.Features.Products;

public sealed record ProductSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    decimal BasePrice,
    string CurrencyCode,
    string? PrimaryImageUrl,
    Guid CategoryId,
    Guid BrandId,
    bool IsDigital,
    bool IsActive)
{
    public static ProductSummaryDto FromProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var primaryImageUrl = product.Images.FirstOrDefault(image => image.IsPrimary)?.Url
            ?? product.Images.FirstOrDefault()?.Url;

        return new ProductSummaryDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Sku,
            product.BasePrice,
            product.CurrencyCode,
            primaryImageUrl,
            product.CategoryId,
            product.BrandId,
            product.IsDigital,
            product.IsActive);
    }
}
