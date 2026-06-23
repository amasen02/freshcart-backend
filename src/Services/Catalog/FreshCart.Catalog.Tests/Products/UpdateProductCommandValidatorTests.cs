using FluentValidation.TestHelper;
using FreshCart.Catalog.Api.Features.Products;
using FreshCart.Catalog.Api.Features.Products.UpdateProduct;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Tests.Products;

public sealed class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator validator = new();

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

    [Fact]
    public void RejectsEmptyProductIdentifier()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.ProductId);
    }

    [Fact]
    public void RejectsMissingName()
    {
        var command = CreateValidCommand() with { Name = string.Empty };

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
    [InlineData(0)]
    [InlineData(-10)]
    public void RejectsNonPositiveBasePrice(int basePrice)
    {
        var command = CreateValidCommand() with { BasePrice = basePrice };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.BasePrice);
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("EURO")]
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

    private static UpdateProductCommand CreateValidCommand() => new(
        ProductId: Guid.NewGuid(),
        Name: "TaskFlow Pro 2026",
        Description: "Project planning suite.",
        BasePrice: 89.99m,
        CurrencyCode: "USD",
        CategoryId: Guid.NewGuid(),
        BrandId: Guid.NewGuid(),
        IsDigital: true,
        IsActive: true);
}
