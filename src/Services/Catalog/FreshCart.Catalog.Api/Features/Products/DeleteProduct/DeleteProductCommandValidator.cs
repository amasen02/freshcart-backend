using FluentValidation;

namespace FreshCart.Catalog.Api.Features.Products.DeleteProduct;

public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
    }
}
