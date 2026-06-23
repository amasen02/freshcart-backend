using FluentValidation.TestHelper;
using FreshCart.Catalog.Api.Features.Products.DeleteProduct;

namespace FreshCart.Catalog.Tests.Products;

public sealed class DeleteProductCommandValidatorTests
{
    private readonly DeleteProductCommandValidator validator = new();

    [Fact]
    public void AcceptsANonEmptyProductIdentifier()
    {
        validator.TestValidate(new DeleteProductCommand(Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RejectsAnEmptyProductIdentifier()
    {
        validator.TestValidate(new DeleteProductCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(invalid => invalid.ProductId);
    }
}
