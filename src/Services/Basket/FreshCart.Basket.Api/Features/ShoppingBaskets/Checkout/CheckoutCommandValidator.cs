using FluentValidation;
using FreshCart.Basket.Api.Persistence;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;

/// <summary>
/// Structural checkout rules plus the one stateful rule: a basket holding physical items cannot
/// check out without a shipping address. The basket read goes through the cached repository, so
/// the handler's own read moments later is served from the in-process cache.
/// </summary>
public sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    private readonly IBasketRepository basketRepository;

    public CheckoutCommandValidator(IBasketRepository basketRepository)
    {
        this.basketRepository = basketRepository;

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

        RuleFor(command => command.ShippingAddress)
            .MustAsync(async (command, _, cancellationToken) =>
                await BasketHasNoPhysicalItemsAsync(command.CustomerId, cancellationToken).ConfigureAwait(false))
            .When(command => command.ShippingAddress is null)
            .WithMessage("A shipping address is required because the basket contains physical items.");
    }

    private async Task<bool> BasketHasNoPhysicalItemsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customerBasket = await basketRepository.GetAsync(customerId, cancellationToken).ConfigureAwait(false);
        return customerBasket is null || !customerBasket.ContainsPhysicalItems;
    }
}
