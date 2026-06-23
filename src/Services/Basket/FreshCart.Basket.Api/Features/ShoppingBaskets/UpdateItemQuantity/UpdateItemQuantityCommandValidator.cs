using FluentValidation;
using FreshCart.Basket.Api.Domain;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.UpdateItemQuantity;

public sealed class UpdateItemQuantityCommandValidator : AbstractValidator<UpdateItemQuantityCommand>
{
    public UpdateItemQuantityCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.ProductId).NotEmpty();

        RuleFor(command => command.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Quantity cannot be negative; send 0 to remove the line.")
            .LessThanOrEqualTo(ShoppingBasket.MaxQuantityPerLine)
            .WithMessage($"A basket line cannot hold more than {ShoppingBasket.MaxQuantityPerLine} units.");
    }
}
