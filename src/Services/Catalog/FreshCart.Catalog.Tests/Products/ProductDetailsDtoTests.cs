using FluentAssertions;
using FreshCart.Catalog.Api.Features.Products.GetProduct;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Tests.Products;

public sealed class ProductDetailsDtoTests
{
    [Fact]
    public void PrefersTheImageMarkedPrimaryForTheHeroImageUrl()
    {
        var product = CreateProduct(
            new ProductImage("https://cdn.freshcart.test/gallery.png", "Gallery", IsPrimary: false),
            new ProductImage("https://cdn.freshcart.test/hero.png", "Hero", IsPrimary: true));

        var productDetails = ProductDetailsDto.FromProduct(product, "Software", "NovaSoft");

        productDetails.ImageUrl.Should().Be("https://cdn.freshcart.test/hero.png");
    }

    [Fact]
    public void FallsBackToTheFirstImageWhenNoneIsMarkedPrimary()
    {
        var product = CreateProduct(
            new ProductImage("https://cdn.freshcart.test/first.png", "First", IsPrimary: false),
            new ProductImage("https://cdn.freshcart.test/second.png", "Second", IsPrimary: false));

        var productDetails = ProductDetailsDto.FromProduct(product, "Software", "NovaSoft");

        productDetails.ImageUrl.Should().Be("https://cdn.freshcart.test/first.png");
    }

    [Fact]
    public void LeavesTheHeroImageNullWhenTheProductHasNoImages()
    {
        var product = CreateProduct();

        var productDetails = ProductDetailsDto.FromProduct(product, "Software", "NovaSoft");

        productDetails.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void CarriesTheResolvedCategoryAndBrandNamesAndTheActiveFlag()
    {
        var product = CreateProduct();
        product.IsActive = false;

        var productDetails = ProductDetailsDto.FromProduct(product, "Software", "NovaSoft");

        productDetails.PrimaryCategory.Should().Be("Software");
        productDetails.BrandName.Should().Be("NovaSoft");
        productDetails.IsActive.Should().BeFalse();
        productDetails.Price.Should().Be(product.BasePrice);
    }

    private static Product CreateProduct(params ProductImage[] images) => new()
    {
        Id = Guid.NewGuid(),
        Name = "TaskFlow Pro 2026",
        Slug = "taskflow-pro-2026",
        Sku = "FC-SW-0100",
        BasePrice = 89.99m,
        CurrencyCode = "USD",
        CategoryId = Guid.NewGuid(),
        BrandId = Guid.NewGuid(),
        IsActive = true,
        IsDigital = true,
        Images = [.. images],
    };
}
