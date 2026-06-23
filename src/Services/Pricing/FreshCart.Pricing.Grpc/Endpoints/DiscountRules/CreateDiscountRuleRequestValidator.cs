using FluentValidation;
using FreshCart.Pricing.Grpc.Models;

namespace FreshCart.Pricing.Grpc.Endpoints.DiscountRules;

public sealed class CreateDiscountRuleRequestValidator : AbstractValidator<CreateDiscountRuleRequest>
{
    private const decimal MaxDiscountPercentage = 100m;

    public CreateDiscountRuleRequestValidator()
    {
        RuleFor(request => request.ProductId)
            .NotEmpty();

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(PricingFieldLengths.DiscountRuleName);

        RuleFor(request => request.DiscountPercentage)
            .GreaterThan(0m)
            .LessThanOrEqualTo(MaxDiscountPercentage);

        RuleFor(request => request.ValidToUtc)
            .GreaterThan(request => request.ValidFromUtc)
            .WithMessage("ValidToUtc must be after ValidFromUtc.");
    }
}
