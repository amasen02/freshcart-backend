using FluentValidation;
using FreshCart.Basket.Api.Domain;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.AddItem;

public sealed class AddItemCommandValidator : AbstractValidator<AddItemCommand>
{
    public AddItemCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.ProductId).NotEmpty();

        RuleFor(command => command.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(ShoppingBasket.MaxQuantityPerLine)
            .WithMessage($"A basket line cannot hold more than {ShoppingBasket.MaxQuantityPerLine} units.");
    }
}
