using FluentValidation.TestHelper;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;
using FreshCart.Basket.Api.Persistence;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Basket.Tests.Support;
using NSubstitute;
using Xunit;

namespace FreshCart.Basket.Tests.Features.Checkout;

public sealed class CheckoutCommandValidatorTests
{
    private static readonly CheckoutAddress ValidAddress = new("12 Market Street", null, "Colombo", "00100", "LK");

    private readonly IBasketRepository _basketRepository = Substitute.For<IBasketRepository>();
    private readonly CheckoutCommandValidator _validator;

    public CheckoutCommandValidatorTests()
    {
        _validator = new CheckoutCommandValidator(_basketRepository);
    }

    [Fact]
    public async Task FullyPopulatedCommandPasses()
    {
        var result = await _validator.TestValidateAsync(CommandWith(shippingAddress: ValidAddress));

        result.ShouldNotHaveAnyValidationErrors();
        await _basketRepository.DidNotReceiveWithAnyArgs().GetAsync(Guid.Empty, CancellationToken.None);
    }

    [Fact]
    public async Task MissingShippingAddressFailsWhenTheBasketHoldsPhysicalItems()
    {
        var command = CommandWith(shippingAddress: null);
        var physicalBasket = ShoppingBasket.CreateForCustomer(command.CustomerId);
        physicalBasket.AddOrMergeItem(TestBasketItems.Create(isDigital: false));
        _basketRepository.GetAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns(physicalBasket);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(checkoutCommand => checkoutCommand.ShippingAddress)
            .WithErrorMessage("A shipping address is required because the basket contains physical items.");
    }

    [Fact]
    public async Task MissingShippingAddressIsAcceptedForADigitalOnlyBasket()
    {
        var command = CommandWith(shippingAddress: null);
        var digitalBasket = ShoppingBasket.CreateForCustomer(command.CustomerId);
        digitalBasket.AddOrMergeItem(TestBasketItems.Create(isDigital: true));
        _basketRepository.GetAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns(digitalBasket);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(checkoutCommand => checkoutCommand.ShippingAddress);
    }

    [Fact]
    public async Task MissingShippingAddressIsAcceptedWhenThereIsNoBasketBecauseTheHandlerRejectsEmptyBaskets()
    {
        var command = CommandWith(shippingAddress: null);
        _basketRepository.GetAsync(command.CustomerId, Arg.Any<CancellationToken>())
            .Returns((ShoppingBasket?)null);

        var result = await _validator.TestValidateAsync(command);

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
