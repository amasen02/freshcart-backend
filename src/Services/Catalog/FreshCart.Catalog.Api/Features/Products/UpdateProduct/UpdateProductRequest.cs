using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Api.Features.Products.UpdateProduct;

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    decimal BasePrice,
    string CurrencyCode,
    Guid CategoryId,
    Guid BrandId,
    bool IsDigital,
    bool IsActive,
    IReadOnlyList<ProductImage>? Images = null,
    IReadOnlyList<ProductAttribute>? Attributes = null);
