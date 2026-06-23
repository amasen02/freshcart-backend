namespace FreshCart.Catalog.Api.Features.Products.GetProducts;

/// <summary>
/// Maps the public query-string sort tokens onto <see cref="ProductSortOption"/>. An absent token
/// falls back to name ordering; an unknown token is rejected so callers get a clear 400 instead of
/// silently unsorted results.
/// </summary>
public static class ProductSortOptionParser
{
    public const string NameToken = "name";
    public const string PriceAscendingToken = "price-asc";
    public const string PriceDescendingToken = "price-desc";
    public const string NewestToken = "newest";

    public static readonly string AllowedTokensDescription =
        $"{NameToken}, {PriceAscendingToken}, {PriceDescendingToken}, {NewestToken}";

    public static bool TryParse(string? sortToken, out ProductSortOption sortOption)
    {
        if (string.IsNullOrWhiteSpace(sortToken))
        {
            sortOption = ProductSortOption.Name;
            return true;
        }

        var normalisedToken = sortToken.Trim();

        if (string.Equals(normalisedToken, NameToken, StringComparison.OrdinalIgnoreCase))
        {
            sortOption = ProductSortOption.Name;
            return true;
        }

        if (string.Equals(normalisedToken, PriceAscendingToken, StringComparison.OrdinalIgnoreCase))
        {
            sortOption = ProductSortOption.PriceAscending;
            return true;
        }

        if (string.Equals(normalisedToken, PriceDescendingToken, StringComparison.OrdinalIgnoreCase))
        {
            sortOption = ProductSortOption.PriceDescending;
            return true;
        }

        if (string.Equals(normalisedToken, NewestToken, StringComparison.OrdinalIgnoreCase))
        {
            sortOption = ProductSortOption.Newest;
            return true;
        }

        sortOption = ProductSortOption.Name;
        return false;
    }
}
