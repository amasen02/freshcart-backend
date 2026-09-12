using FluentValidation.TestHelper;
using FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using Xunit;

namespace FreshCart.Basket.Tests.Features.Checkout;

public sealed class CheckoutAddressValidatorTests
{
    private const int MaxAddressLineLength = 200;

    private static readonly CheckoutAddress ValidAddress = new("12 Market Street", "Apartment 4B", "Colombo", "00100", "LK");

    private readonly CheckoutAddressValidator _validator = new();

    [Fact]
    public void CompleteAddressPasses()
    {
        var result = _validator.TestValidate(ValidAddress);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SecondLineIsOptional()
    {
        var result = _validator.TestValidate(ValidAddress with { Line2 = null });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyFirstLineFails()
    {
        var result = _validator.TestValidate(ValidAddress with { Line1 = string.Empty });

        result.ShouldHaveValidationErrorFor(address => address.Line1);
    }

    [Fact]
    public void OverlongLinesFail()
    {
        var overlongLine = new string('A', MaxAddressLineLength + 1);
        var result = _validator.TestValidate(ValidAddress with { Line1 = overlongLine, Line2 = overlongLine });

        result.ShouldHaveValidationErrorFor(address => address.Line1);
        result.ShouldHaveValidationErrorFor(address => address.Line2);
    }

    [Fact]
    public void EmptyCityFails()
    {
        var result = _validator.TestValidate(ValidAddress with { City = string.Empty });

        result.ShouldHaveValidationErrorFor(address => address.City);
    }

    [Fact]
    public void EmptyPostalCodeFails()
    {
        var result = _validator.TestValidate(ValidAddress with { PostalCode = string.Empty });

        result.ShouldHaveValidationErrorFor(address => address.PostalCode);
    }

    [Theory]
    [InlineData("GB", "W1K 7TN")]
    [InlineData("US", "00501-1234")]
    [InlineData("IE", "D04 K7X4")]
    [InlineData("DE", "10115")]
    [InlineData("FR", "75001")]
    [InlineData("AU", "2000")]
    public void SupportedCountryPostalShapesPass(string countryCode, string postalCode)
    {
        var result = _validator.TestValidate(ValidAddress with { CountryCode = countryCode, PostalCode = postalCode });

        result.ShouldNotHaveValidationErrorFor(address => address.PostalCode);
    }

    [Theory]
    [InlineData("GB", "12345")]
    [InlineData("US", "1234")]
    [InlineData("IE", "D04- K7X4")]
    [InlineData("DE", "1011")]
    [InlineData("FR", "7500A")]
    [InlineData("AU", "20000")]
    public void MalformedSupportedCountryPostalShapesFail(string countryCode, string postalCode)
    {
        var result = _validator.TestValidate(ValidAddress with { CountryCode = countryCode, PostalCode = postalCode });

        result.ShouldHaveValidationErrorFor(address => address.PostalCode);
    }

    [Theory]
    [InlineData("L")]
    [InlineData("LKA")]
    public void CountryCodeMustBeTwoLetters(string countryCode)
    {
        var result = _validator.TestValidate(ValidAddress with { CountryCode = countryCode });

        result.ShouldHaveValidationErrorFor(address => address.CountryCode);
    }
}
