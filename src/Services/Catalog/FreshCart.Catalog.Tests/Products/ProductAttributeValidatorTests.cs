using FluentValidation.TestHelper;
using FreshCart.Catalog.Api.Features.Products;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Tests.Products;

public sealed class ProductAttributeValidatorTests
{
    private readonly ProductAttributeValidator validator = new();

    [Fact]
    public void AcceptsANamedValuePair()
    {
        validator.TestValidate(new ProductAttribute("Platform", "Windows, macOS"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RejectsAnEmptyName()
    {
        validator.TestValidate(new ProductAttribute(string.Empty, "Windows"))
            .ShouldHaveValidationErrorFor(invalid => invalid.Name);
    }

    [Fact]
    public void RejectsAnEmptyValue()
    {
        validator.TestValidate(new ProductAttribute("Platform", string.Empty))
            .ShouldHaveValidationErrorFor(invalid => invalid.Value);
    }

    [Fact]
    public void RejectsNameLongerThanTheLimit()
    {
        validator.TestValidate(new ProductAttribute(new string('a', ProductConstraints.MaxAttributeNameLength + 1), "Windows"))
            .ShouldHaveValidationErrorFor(invalid => invalid.Name);
    }

    [Fact]
    public void RejectsValueLongerThanTheLimit()
    {
        validator.TestValidate(new ProductAttribute("Platform", new string('a', ProductConstraints.MaxAttributeValueLength + 1)))
            .ShouldHaveValidationErrorFor(invalid => invalid.Value);
    }
}
