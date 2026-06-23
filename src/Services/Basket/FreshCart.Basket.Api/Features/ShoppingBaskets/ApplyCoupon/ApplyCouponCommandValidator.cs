using FluentValidation;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.ApplyCoupon;

public sealed class ApplyCouponCommandValidator : AbstractValidator<ApplyCouponCommand>
{
    private const int MaxCouponCodeLength = 64;

    public ApplyCouponCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();

        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(MaxCouponCodeLength);
    }
}
