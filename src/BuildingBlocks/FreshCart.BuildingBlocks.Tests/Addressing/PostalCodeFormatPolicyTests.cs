using FluentAssertions;
using FreshCart.BuildingBlocks.Addressing;

namespace FreshCart.BuildingBlocks.Tests.Addressing;

public sealed class PostalCodeFormatPolicyTests
{
    [Theory]
    [InlineData("GB", "W1K 7TN")]
    [InlineData("GB", "SW1A1AA")]
    [InlineData("US", "00501")]
    [InlineData("IE", "D04 K7X4")]
    [InlineData("DE", "10115")]
    [InlineData("FR", "75001")]
    [InlineData("AU", "2000")]
    public void AcceptsTheSupportedNationalPostalShapes(string countryCode, string postalCode)
    {
        PostalCodeFormatPolicy.IsValid(countryCode, postalCode).Should().BeTrue();
    }

    [Theory]
    [InlineData("GB", "12345")]
    [InlineData("US", "1234")]
    [InlineData("IE", "D04- K7X4")]
    [InlineData("DE", "1011")]
    [InlineData("FR", "7500A")]
    [InlineData("AU", "20000")]
    [InlineData("US", "１２３４５")]
    [InlineData("US", "١٢٣٤٥")]
    [InlineData("GB", "SW1A\n1AA")]
    public void RejectsMalformedNationalPostalShapes(string countryCode, string postalCode)
    {
        PostalCodeFormatPolicy.IsValid(countryCode, postalCode).Should().BeFalse();
    }

    [Fact]
    public void LeavesUnknownCountryCodesToTheirExistingGenericValidation()
    {
        PostalCodeFormatPolicy.IsValid("LK", "00100").Should().BeTrue();
    }
}
