using FluentAssertions;
using FreshCart.Catalog.Api.Features.Products.GetProducts;

namespace FreshCart.Catalog.Tests.Products;

public sealed class ProductSortOptionParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AbsentTokenFallsBackToNameOrdering(string? sortToken)
    {
        var parsed = ProductSortOptionParser.TryParse(sortToken, out var sortOption);

        parsed.Should().BeTrue();
        sortOption.Should().Be(ProductSortOption.Name);
    }

    [Theory]
    [InlineData("name", ProductSortOption.Name)]
    [InlineData("price-asc", ProductSortOption.PriceAscending)]
    [InlineData("price-desc", ProductSortOption.PriceDescending)]
    [InlineData("newest", ProductSortOption.Newest)]
    [InlineData("PRICE-ASC", ProductSortOption.PriceAscending)]
    [InlineData(" Newest ", ProductSortOption.Newest)]
    public void ParsesEveryPublicTokenCaseInsensitivelyAndTrimmed(string sortToken, ProductSortOption expectedSortOption)
    {
        var parsed = ProductSortOptionParser.TryParse(sortToken, out var sortOption);

        parsed.Should().BeTrue();
        sortOption.Should().Be(expectedSortOption);
    }

    [Theory]
    [InlineData("price")]
    [InlineData("oldest")]
    [InlineData("name;drop table")]
    public void UnknownTokensAreRejectedSoCallersGetAClearBadRequest(string sortToken)
    {
        var parsed = ProductSortOptionParser.TryParse(sortToken, out _);

        parsed.Should().BeFalse();
    }
}
