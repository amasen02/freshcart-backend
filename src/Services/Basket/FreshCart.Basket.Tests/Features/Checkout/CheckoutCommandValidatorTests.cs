using FluentValidation.TestHelper;
using FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using Xunit;

namespace FreshCart.Basket.Tests.Features.Checkout;

public sealed class CheckoutCommandValidatorTests
{
    private static readonly CheckoutAddress ValidAddress = new("12 Market Street", null, "Colombo", "00100", "LK");

    private readonly CheckoutCommandValidator _validator = new();

    [Fact]
    public async Task FullyPopulatedCommandPasses()
    {
        var result = await _validator.TestValidateAsync(CommandWith(shippingAddress: ValidAddress));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task AMissingShippingAddressIsStructurallyValidBecauseThePhysicalItemsRuleLivesInTheHandler()
    {
        var result = await _validator.TestValidateAsync(CommandWith(shippingAddress: null));

        result.ShouldNotHaveValidationErrorFor(checkoutCommand => checkoutCommand.ShippingAddress);
    }

    [Fact]
    public async Task UnsupportedPaymentMethodFails()
    {
        var command = CommandWith(shippingAddress: ValidAddress) with { PaymentMethod = "Barter" };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(checkoutCommand => checkoutCommand.PaymentMethod);
    }

    [Fact]
    public async Task MalformedEmailFails()
    {
        var command = CommandWith(shippingAddress: ValidAddress) with { CustomerEmail = "not-an-email" };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(checkoutCommand => checkoutCommand.CustomerEmail);
    }

    [Fact]
    public async Task EmptyDisplayNameFails()
    {
        var command = CommandWith(shippingAddress: ValidAddress) with { CustomerDisplayName = string.Empty };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(checkoutCommand => checkoutCommand.CustomerDisplayName);
    }

    [Fact]
    public async Task InvalidBillingAddressFailsOnTheNestedRule()
    {
        var command = CommandWith(shippingAddress: ValidAddress) with
        {
            BillingAddress = ValidAddress with { CountryCode = "LKA" },
        };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("BillingAddress.CountryCode");
    }

    [Fact]
    public async Task ProvidedShippingAddressIsValidatedWithTheSameAddressRules()
    {
        var command = CommandWith(shippingAddress: ValidAddress with { City = string.Empty });

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("ShippingAddress.City");
    }

    private static CheckoutCommand CommandWith(CheckoutAddress? shippingAddress) => new(
        Guid.NewGuid(),
        "shopper@freshcart.local",
        "Sam Shopper",
        PaymentMethods.CreditCard,
        ValidAddress,
        shippingAddress);
}
