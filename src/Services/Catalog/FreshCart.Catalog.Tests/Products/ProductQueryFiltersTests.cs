using FluentAssertions;
using FreshCart.Catalog.Api.Features.Products.GetProducts;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Tests.Products;

public sealed class ProductQueryFiltersTests
{
    private static readonly Guid SoftwareCategoryId = Guid.NewGuid();
    private static readonly Guid GamesCategoryId = Guid.NewGuid();
    private static readonly Guid NovaSoftBrandId = Guid.NewGuid();
    private static readonly Guid PixelForgeBrandId = Guid.NewGuid();

    [Fact]
    public void ExcludesInactiveProductsForStorefrontCallers()
    {
        var products = BuildCatalog().AsQueryable();

        var filteredSkus = ProductQueryFilters
            .ApplyFilters(products, categoryId: null, brandId: null, maxPrice: null, isDigital: null, includeInactive: false)
            .Select(product => product.Sku);

        filteredSkus.Should().NotContain("FC-SW-0003");
    }

    [Fact]
    public void IncludesInactiveProductsForBackOfficeCallers()
    {
        var products = BuildCatalog().AsQueryable();

        var filteredSkus = ProductQueryFilters
            .ApplyFilters(products, categoryId: null, brandId: null, maxPrice: null, isDigital: null, includeInactive: true)
            .Select(product => product.Sku);

        filteredSkus.Should().Contain("FC-SW-0003");
    }

    [Fact]
    public void FiltersByCategoryBrandMaxPriceAndDigitalFlagSimultaneously()
    {
        var products = BuildCatalog().AsQueryable();

        var filteredProducts = ProductQueryFilters
            .ApplyFilters(
                products,
                categoryId: SoftwareCategoryId,
                brandId: NovaSoftBrandId,
                maxPrice: 100m,
                isDigital: true,
                includeInactive: false)
            .ToList();

        filteredProducts.Should().ContainSingle().Which.Sku.Should().Be("FC-SW-0001");
    }

    [Fact]
    public void MaxPriceBoundaryIsInclusive()
    {
        var products = BuildCatalog().AsQueryable();

        var filteredSkus = ProductQueryFilters
            .ApplyFilters(products, categoryId: null, brandId: null, maxPrice: 89.99m, isDigital: null, includeInactive: false)
            .Select(product => product.Sku)
            .ToList();

        filteredSkus.Should().Contain("FC-SW-0001");
        filteredSkus.Should().NotContain("FC-SW-0002");
    }

    [Fact]
    public void SortsByNameThenSkuAsTheDefaultStorefrontOrder()
    {
        var products = BuildCatalog().AsQueryable();

        var sortedNames = ProductQueryFilters
            .ApplySort(products, ProductSortOption.Name)
            .Select(product => product.Name)
            .ToList();

        sortedNames.Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public void SortsByPriceAscendingWithSkuTieBreakForDeterministicPaging()
    {
        var products = BuildCatalog().AsQueryable();

        var sortedProducts = ProductQueryFilters
            .ApplySort(products, ProductSortOption.PriceAscending)
            .ToList();

        sortedProducts.Select(product => product.BasePrice).Should().BeInAscendingOrder();
        sortedProducts.Where(product => product.BasePrice == 39.99m)
            .Select(product => product.Sku)
            .Should().ContainInOrder("FC-GM-0003", "FC-GM-0004");
    }

    [Fact]
    public void SortsByPriceDescending()
    {
        var products = BuildCatalog().AsQueryable();

        var sortedPrices = ProductQueryFilters
            .ApplySort(products, ProductSortOption.PriceDescending)
            .Select(product => product.BasePrice)
            .ToList();

        sortedPrices.Should().BeInDescendingOrder();
    }

    [Fact]
    public void SortsByNewestFirst()
    {
        var products = BuildCatalog().AsQueryable();

        var sortedCreationTimes = ProductQueryFilters
            .ApplySort(products, ProductSortOption.Newest)
            .Select(product => product.CreatedOnUtc)
            .ToList();

        sortedCreationTimes.Should().BeInDescendingOrder();
    }

    [Fact]
    public void RejectsAnUnknownSortOptionInsteadOfReturningUnsortedResults()
    {
        var products = BuildCatalog().AsQueryable();

        var sorting = () => ProductQueryFilters.ApplySort(products, (ProductSortOption)99);

        sorting.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static List<Product> BuildCatalog()
    {
        var baselineUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        return
        [
            CreateProduct("FC-SW-0001", "TaskFlow Pro", 89.99m, SoftwareCategoryId, NovaSoftBrandId, isActive: true, isDigital: true, baselineUtc.AddDays(1)),
            CreateProduct("FC-SW-0002", "PhotoSmith Studio", 129.99m, SoftwareCategoryId, NovaSoftBrandId, isActive: true, isDigital: true, baselineUtc.AddDays(2)),
            CreateProduct("FC-SW-0003", "Retired Utility", 9.99m, SoftwareCategoryId, NovaSoftBrandId, isActive: false, isDigital: true, baselineUtc.AddDays(3)),
            CreateProduct("FC-GM-0003", "Neon Drift Racing", 39.99m, GamesCategoryId, PixelForgeBrandId, isActive: true, isDigital: true, baselineUtc.AddDays(4)),
            CreateProduct("FC-GM-0004", "Dungeon of Echoes", 39.99m, GamesCategoryId, PixelForgeBrandId, isActive: true, isDigital: false, baselineUtc.AddDays(5)),
        ];
    }

    private static Product CreateProduct(
        string sku,
        string name,
        decimal basePrice,
        Guid categoryId,
        Guid brandId,
        bool isActive,
        bool isDigital,
        DateTimeOffset createdOnUtc) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = sku.ToLowerInvariant(),
        Sku = sku,
        BasePrice = basePrice,
        CurrencyCode = "USD",
        CategoryId = categoryId,
        BrandId = brandId,
        IsActive = isActive,
        IsDigital = isDigital,
        CreatedOnUtc = createdOnUtc,
        UpdatedOnUtc = createdOnUtc,
    };
}
