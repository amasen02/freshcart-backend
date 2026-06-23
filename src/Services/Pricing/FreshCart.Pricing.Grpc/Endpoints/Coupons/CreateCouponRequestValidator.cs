using FluentValidation;
using FreshCart.Pricing.Grpc.Models;

namespace FreshCart.Pricing.Grpc.Endpoints.Coupons;

public sealed class CreateCouponRequestValidator : AbstractValidator<CreateCouponRequest>
{
    private const string CouponCodePattern = "^[A-Z0-9]+$";
    private const decimal MaxPercentageDiscountValue = 100m;

    public CreateCouponRequestValidator()
    {
        RuleFor(request => request.Code)
            .NotEmpty()
            .MaximumLength(PricingFieldLengths.CouponCode)
            .Matches(CouponCodePattern)
            .WithMessage("Code must contain only uppercase letters and digits.");

        RuleFor(request => request.DiscountType)
            .IsInEnum();

        RuleFor(request => request.DiscountValue)
            .GreaterThan(0m);

        RuleFor(request => request.DiscountValue)
            .LessThanOrEqualTo(MaxPercentageDiscountValue)
            .When(request => request.DiscountType == CouponDiscountType.Percentage)
            .WithMessage("Percentage coupons cannot exceed 100 percent.");

        RuleFor(request => request.MinimumOrderAmount)
            .GreaterThan(0m);

        RuleFor(request => request.UsageLimit)
            .GreaterThan(0);

        RuleFor(request => request.ValidToUtc)
            .GreaterThan(request => request.ValidFromUtc)
            .WithMessage("ValidToUtc must be after ValidFromUtc.");
    }
}
