namespace FreshCart.Catalog.Api.Models;

/// <summary>
/// Catalog product document. <see cref="InitialStockQuantity"/> only feeds the ProductCreated
/// integration event so Inventory can seed its stock row; live stock is owned by Inventory.
/// </summary>
public sealed class Product
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public string? Description { get; set; }

    public required string Sku { get; init; }

    public decimal BasePrice { get; set; }

    public required string CurrencyCode { get; set; }

    public Guid CategoryId { get; set; }

    public Guid BrandId { get; set; }

    public bool IsActive { get; set; }

    public bool IsDigital { get; set; }

    public IList<ProductImage> Images { get; set; } = [];

    public IList<ProductAttribute> Attributes { get; set; } = [];

    public int InitialStockQuantity { get; init; }

    public DateTimeOffset CreatedOnUtc { get; init; }

    public DateTimeOffset UpdatedOnUtc { get; set; }
}
