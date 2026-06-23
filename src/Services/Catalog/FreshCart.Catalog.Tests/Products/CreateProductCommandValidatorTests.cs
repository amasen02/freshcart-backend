using FluentValidation.TestHelper;
using FreshCart.Catalog.Api.Features.Products;
using FreshCart.Catalog.Api.Features.Products.CreateProduct;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Tests.Products;

public sealed class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator validator = new();

    [Fact]
    public void AcceptsAFullyPopulatedValidCommand()
    {
        var command = CreateValidCommand() with
        {
            Images = [new ProductImage("https://cdn.freshcart.test/img.png", "Box art", IsPrimary: true)],
            Attributes = [new ProductAttribute("Platform", "PC")],
        };

        validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RejectsMissingName(string name)
    {
        var command = CreateValidCommand() with { Name = name };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Name);
    }

    [Fact]
    public void RejectsNameLongerThanTheLimit()
    {
        var command = CreateValidCommand() with { Name = new string('a', ProductConstraints.MaxNameLength + 1) };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Name);
    }

    [Fact]
    public void RejectsDescriptionLongerThanTheLimit()
    {
        var command = CreateValidCommand() with
        {
            Description = new string('a', ProductConstraints.MaxDescriptionLength + 1),
        };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("fc-sw-0001")]
    [InlineData("FC--SW-0001")]
    [InlineData("FC-SW-0001-")]
    [InlineData("-FC-SW-0001")]
    [InlineData("FC SW 0001")]
    public void RejectsMalformedSkus(string sku)
    {
        var command = CreateValidCommand() with { Sku = sku };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Sku);
    }

    [Theory]
    [InlineData("FC-SW-0001")]
    [InlineData("SINGLEGROUP")]
    [InlineData("A1-B2-C3-D4")]
    public void AcceptsWellFormedSkus(string sku)
    {
        var command = CreateValidCommand() with { Sku = sku };

        validator.TestValidate(command).ShouldNotHaveValidationErrorFor(valid => valid.Sku);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsNonPositiveBasePrice(int basePrice)
    {
        var command = CreateValidCommand() with { BasePrice = basePrice };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.BasePrice);
    }

    [Fact]
    public void RejectsBasePriceAboveTheCeiling()
    {
        var command = CreateValidCommand() with { BasePrice = ProductConstraints.MaximumBasePrice + 0.01m };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.BasePrice);
    }

    [Theory]
    [InlineData("")]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("U5D")]
    public void RejectsCurrencyCodesThatAreNotThreeUppercaseLetters(string currencyCode)
    {
        var command = CreateValidCommand() with { CurrencyCode = currencyCode };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.CurrencyCode);
    }

    [Fact]
    public void RejectsEmptyCategoryAndBrandIdentifiers()
    {
        var command = CreateValidCommand() with { CategoryId = Guid.Empty, BrandId = Guid.Empty };

        var validationResult = validator.TestValidate(command);

        validationResult.ShouldHaveValidationErrorFor(invalid => invalid.CategoryId);
        validationResult.ShouldHaveValidationErrorFor(invalid => invalid.BrandId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(ProductConstraints.MaxInitialStockQuantity + 1)]
    public void RejectsInitialStockQuantityOutsideTheAllowedRange(int initialStockQuantity)
    {
        var command = CreateValidCommand() with { InitialStockQuantity = initialStockQuantity };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.InitialStockQuantity);
    }

    [Fact]
    public void RejectsMoreImagesThanTheLimit()
    {
        var tooManyImages = Enumerable.Range(0, ProductConstraints.MaxImageCount + 1)
            .Select(imageIndex => new ProductImage(
                $"https://cdn.freshcart.test/{imageIndex}.png",
                "Gallery image",
                IsPrimary: false))
            .ToList();
        var command = CreateValidCommand() with { Images = tooManyImages };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Images);
    }

    [Fact]
    public void RejectsTwoImagesBothMarkedPrimary()
    {
        var command = CreateValidCommand() with
        {
            Images =
            [
                new ProductImage("https://cdn.freshcart.test/1.png", "Front", IsPrimary: true),
                new ProductImage("https://cdn.freshcart.test/2.png", "Back", IsPrimary: true),
            ],
        };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Images);
    }

    [Fact]
    public void RejectsMoreAttributesThanTheLimit()
    {
        var tooManyAttributes = Enumerable.Range(0, ProductConstraints.MaxAttributeCount + 1)
            .Select(attributeIndex => new ProductAttribute($"Name{attributeIndex}", "Value"))
            .ToList();
        var command = CreateValidCommand() with { Attributes = tooManyAttributes };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Attributes);
    }

    private static CreateProductCommand CreateValidCommand() => new(
        Name: "TaskFlow Pro 2026",
        Description: "Project planning suite.",
        Sku: "FC-SW-0100",
        BasePrice: 89.99m,
        CurrencyCode: "USD",
        CategoryId: Guid.NewGuid(),
        BrandId: Guid.NewGuid(),
        IsDigital: true,
        InitialStockQuantity: 250);
}
