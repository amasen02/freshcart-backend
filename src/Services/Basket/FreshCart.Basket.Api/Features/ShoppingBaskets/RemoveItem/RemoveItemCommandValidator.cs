using FluentValidation;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.RemoveItem;

public sealed class RemoveItemCommandValidator : AbstractValidator<RemoveItemCommand>
{
    public RemoveItemCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.ProductId).NotEmpty();
    }
}
