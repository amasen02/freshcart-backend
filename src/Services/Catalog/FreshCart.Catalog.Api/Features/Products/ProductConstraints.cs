namespace FreshCart.Catalog.Api.Features.Products;

/// <summary>
/// Domain limits shared by the create and update product validators and the route-level guards.
/// </summary>
public static class ProductConstraints
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 4000;
    public const int MaxSkuLength = 50;
    public const int MaxImageCount = 10;
    public const int MaxImageUrlLength = 500;
    public const int MaxImageAltTextLength = 200;
    public const int MaxAttributeCount = 50;
    public const int MaxAttributeNameLength = 100;
    public const int MaxAttributeValueLength = 500;
    public const int MaxInitialStockQuantity = 100_000;
    public const int MaxIdOrSlugLength = 160;
    public const int MaxSearchTermLength = 200;

    public static readonly decimal MaximumBasePrice = 1_000_000m;
}
