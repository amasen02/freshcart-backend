using FluentValidation;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;

public sealed class CheckoutAddressValidator : AbstractValidator<CheckoutAddress>
{
    private const int MaxAddressLineLength = 200;
    private const int MaxCityLength = 100;
    private const int MaxPostalCodeLength = 20;
    private const int IsoCountryCodeLength = 2;

    public CheckoutAddressValidator()
    {
        RuleFor(address => address.Line1)
            .NotEmpty()
            .MaximumLength(MaxAddressLineLength);

        RuleFor(address => address.Line2)
            .MaximumLength(MaxAddressLineLength);

        RuleFor(address => address.City)
            .NotEmpty()
            .MaximumLength(MaxCityLength);

        RuleFor(address => address.PostalCode)
            .NotEmpty()
            .MaximumLength(MaxPostalCodeLength);

        RuleFor(address => address.CountryCode)
            .NotEmpty()
            .Length(IsoCountryCodeLength)
            .WithMessage("Country code must be a two-letter ISO 3166-1 alpha-2 code.");
    }
}
