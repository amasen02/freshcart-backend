using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Api.Features.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string? Description,
    string Sku,
    decimal BasePrice,
    string CurrencyCode,
    Guid CategoryId,
    Guid BrandId,
    bool IsDigital,
    int InitialStockQuantity,
    IReadOnlyList<ProductImage>? Images = null,
    IReadOnlyList<ProductAttribute>? Attributes = null) : ICommand<CreateProductResult>;
