using FluentValidation;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;

/// <summary>
/// Structural checkout rules. The one stateful rule — a basket holding physical items needs a
/// shipping address — lives in the handler, which already reads the basket. Keeping it out of the
/// validator avoids a singleton validator (Carter registers validators as singletons) capturing the
/// scoped basket repository, which the DI container rejects as a captive dependency on startup.
/// </summary>
public sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();

        RuleFor(command => command.CustomerEmail)
            .NotEmpty()
            .EmailAddress();

        RuleFor(command => command.CustomerDisplayName).NotEmpty();

        RuleFor(command => command.PaymentMethod)
            .NotEmpty()
            .Must(PaymentMethods.IsSupported)
            .WithMessage($"Payment method must be one of: {string.Join(", ", PaymentMethods.All)}.");

        RuleFor(command => command.BillingAddress)
            .NotNull()
            .SetValidator(new CheckoutAddressValidator());

        When(command => command.ShippingAddress is not null, () =>
            RuleFor(command => command.ShippingAddress!).SetValidator(new CheckoutAddressValidator()));
    }
}
