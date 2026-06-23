using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Api.Features.Products.GetProducts;

/// <summary>
/// Pure queryable composition for the product listing. Kept free of Marten-specific operators so
/// the exact filter and ordering semantics are unit-testable against in-memory sequences.
/// </summary>
public static class ProductQueryFilters
{
    public static IQueryable<Product> ApplyFilters(
        IQueryable<Product> products,
        Guid? categoryId,
        Guid? brandId,
        decimal? maxPrice,
        bool? isDigital,
        bool includeInactive)
    {
        ArgumentNullException.ThrowIfNull(products);

        if (!includeInactive)
        {
            products = products.Where(product => product.IsActive);
        }

        if (categoryId is { } categoryIdValue)
        {
            products = products.Where(product => product.CategoryId == categoryIdValue);
        }

        if (brandId is { } brandIdValue)
        {
            products = products.Where(product => product.BrandId == brandIdValue);
        }

        if (maxPrice is { } maxPriceValue)
        {
            products = products.Where(product => product.BasePrice <= maxPriceValue);
        }

        if (isDigital is { } isDigitalValue)
        {
            products = products.Where(product => product.IsDigital == isDigitalValue);
        }

        return products;
    }

    public static IQueryable<Product> ApplySort(IQueryable<Product> products, ProductSortOption sortOption)
    {
        ArgumentNullException.ThrowIfNull(products);

        return sortOption switch
        {
            ProductSortOption.Name => products.OrderBy(product => product.Name).ThenBy(product => product.Sku),
            ProductSortOption.PriceAscending => products.OrderBy(product => product.BasePrice).ThenBy(product => product.Sku),
            ProductSortOption.PriceDescending => products.OrderByDescending(product => product.BasePrice).ThenBy(product => product.Sku),
            ProductSortOption.Newest => products.OrderByDescending(product => product.CreatedOnUtc).ThenBy(product => product.Sku),
            _ => throw new ArgumentOutOfRangeException(nameof(sortOption), sortOption, "Unsupported product sort option."),
        };
    }
}
