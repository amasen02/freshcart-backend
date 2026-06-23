using FluentAssertions;
using FreshCart.Catalog.Api.Features.Products;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Tests.Products;

public sealed class ProductSummaryDtoTests
{
    [Fact]
    public void ProjectsTheListingFieldsAndPicksThePrimaryImage()
    {
        var product = CreateProduct(
            new ProductImage("https://cdn.freshcart.test/gallery.png", "Gallery", IsPrimary: false),
            new ProductImage("https://cdn.freshcart.test/hero.png", "Hero", IsPrimary: true));

        var productSummary = ProductSummaryDto.FromProduct(product);

        productSummary.Id.Should().Be(product.Id);
        productSummary.Sku.Should().Be(product.Sku);
        productSummary.BasePrice.Should().Be(product.BasePrice);
        productSummary.PrimaryImageUrl.Should().Be("https://cdn.freshcart.test/hero.png");
    }

    [Fact]
    public void LeavesThePrimaryImageNullWhenTheProductHasNoImages()
    {
        var productSummary = ProductSummaryDto.FromProduct(CreateProduct());

        productSummary.PrimaryImageUrl.Should().BeNull();
    }

    private static Product CreateProduct(params ProductImage[] images) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Harvest Lane Simulator",
        Slug = "harvest-lane-simulator",
        Sku = "FC-GM-0002",
        BasePrice = 29.99m,
        CurrencyCode = "USD",
        CategoryId = Guid.NewGuid(),
        BrandId = Guid.NewGuid(),
        IsActive = true,
        IsDigital = true,
        Images = [.. images],
    };
}
