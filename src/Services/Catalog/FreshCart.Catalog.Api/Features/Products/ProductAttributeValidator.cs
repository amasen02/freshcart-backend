using FluentValidation;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Api.Features.Products;

public sealed class ProductAttributeValidator : AbstractValidator<ProductAttribute>
{
    public ProductAttributeValidator()
    {
        RuleFor(attribute => attribute.Name)
            .NotEmpty()
            .MaximumLength(ProductConstraints.MaxAttributeNameLength);

        RuleFor(attribute => attribute.Value)
            .NotEmpty()
            .MaximumLength(ProductConstraints.MaxAttributeValueLength);
    }
}
