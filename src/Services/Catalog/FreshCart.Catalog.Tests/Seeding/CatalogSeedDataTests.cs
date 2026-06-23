using System.Text.RegularExpressions;
using FluentAssertions;
using FreshCart.Catalog.Api.Seeding;

namespace FreshCart.Catalog.Tests.Seeding;

public sealed partial class CatalogSeedDataTests
{
    private const int RegexTimeoutMilliseconds = 100;

    [Fact]
    public void SeedsExactlySixCategoriesFourBrandsAndTwentyFourProducts()
    {
        CatalogSeedData.Categories.Should().HaveCount(6);
        CatalogSeedData.Brands.Should().HaveCount(4);
        CatalogSeedData.Products.Should().HaveCount(24);
    }

    [Fact]
    public void SeedDataIsDeterministicAcrossAccesses()
    {
        CatalogSeedData.Products.Select(product => product.Id)
            .Should().Equal(CatalogSeedData.Products.Select(product => product.Id));
        CatalogSeedData.Categories.Select(category => category.Id)
            .Should().OnlyHaveUniqueItems();
        CatalogSeedData.Brands.Select(brand => brand.Id)
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void EverySkuIsUniqueAndFollowsTheCatalogFormat()
    {
        var skus = CatalogSeedData.Products.Select(product => product.Sku).ToList();

        skus.Should().OnlyHaveUniqueItems();
        skus.Should().AllSatisfy(sku => SeedSkuFormatRegex().IsMatch(sku).Should().BeTrue());
    }

    [Fact]
    public void EverySlugIsUniqueWithinItsDocumentType()
    {
        CatalogSeedData.Products.Select(product => product.Slug).Should().OnlyHaveUniqueItems();
        CatalogSeedData.Categories.Select(category => category.Slug).Should().OnlyHaveUniqueItems();
        CatalogSeedData.Brands.Select(brand => brand.Slug).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void EveryProductReferencesASeededCategoryAndBrand()
    {
        var categoryIds = CatalogSeedData.Categories.Select(category => category.Id).ToHashSet();
        var brandIds = CatalogSeedData.Brands.Select(brand => brand.Id).ToHashSet();

        CatalogSeedData.Products.Should().AllSatisfy(product =>
        {
            categoryIds.Should().Contain(product.CategoryId);
            brandIds.Should().Contain(product.BrandId);
        });
    }

    [Fact]
    public void EveryCategoryHasExactlyFourProducts()
    {
        CatalogSeedData.Products
            .GroupBy(product => product.CategoryId)
            .Should().HaveCount(6)
            .And.AllSatisfy(productsInCategory => productsInCategory.Should().HaveCount(4));
    }

    [Fact]
    public void EveryProductIsAnActiveDigitalItemWithAPositivePrice()
    {
        CatalogSeedData.Products.Should().AllSatisfy(product =>
        {
            product.IsActive.Should().BeTrue();
            product.IsDigital.Should().BeTrue();
            product.BasePrice.Should().BePositive();
            product.CurrencyCode.Should().Be("USD");
        });
    }

    [Fact]
    public void InitialStockQuantityStaysWithinTheBriefedRange()
    {
        CatalogSeedData.Products.Should().AllSatisfy(product =>
            product.InitialStockQuantity.Should().BeInRange(50, 500));
    }

    [Fact]
    public void EveryProductCarriesOnePrimaryImageSeededFromItsSku()
    {
        CatalogSeedData.Products.Should().AllSatisfy(product =>
        {
            var primaryImage = product.Images.Should().ContainSingle().Subject;
            primaryImage.IsPrimary.Should().BeTrue();
            primaryImage.Url.Should().Be($"https://picsum.photos/seed/{product.Sku}/640/480");
            primaryImage.AltText.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void SeedsTheSixBriefedStorefrontCategoriesInDisplayOrder()
    {
        CatalogSeedData.Categories
            .OrderBy(category => category.SortOrder)
            .Select(category => category.Name)
            .Should().Equal("Software", "Games", "E-Books", "Music", "Gift Cards", "Online Courses");
    }

    [GeneratedRegex(@"^FC-[A-Z]{2}-\d{4}$", RegexOptions.None, RegexTimeoutMilliseconds)]
    private static partial Regex SeedSkuFormatRegex();
}
