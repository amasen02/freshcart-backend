using System.Globalization;
using FluentAssertions;
using FreshCart.Pricing.Grpc.Services;
using Grpc.Core;
using Xunit;

namespace FreshCart.Pricing.Tests.Services;

public sealed class MoneyWireFormatTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("0.01")]
    [InlineData("12.34")]
    [InlineData("19.999")]
    [InlineData("1234567.89")]
    [InlineData("-5.25")]
    public void DecimalRoundTripsExactlyThroughTheWireFormat(string decimalLiteral)
    {
        var originalValue = decimal.Parse(decimalLiteral, CultureInfo.InvariantCulture);

        var roundTrippedValue = MoneyWireFormat.Parse(MoneyWireFormat.ToWire(originalValue), "unit_price");

        roundTrippedValue.Should().Be(originalValue);
    }

    [Fact]
    public void WireFormatUsesDotDecimalSeparatorRegardlessOfCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            MoneyWireFormat.ToWire(10.5m).Should().Be("10.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12.3.4")]
    [InlineData("10,50")]
    [InlineData("1,234.56")]
    public void MalformedWireValueFailsWithInvalidArgument(string malformedValue)
    {
        var parsing = () => MoneyWireFormat.Parse(malformedValue, "order_subtotal");

        parsing.Should().Throw<RpcException>()
            .Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public void ParseFailureNamesTheOffendingField()
    {
        var parsing = () => MoneyWireFormat.Parse("not-a-decimal", "grand_total");

        parsing.Should().Throw<RpcException>()
            .Which.Status.Detail.Should().Contain("grand_total");
    }
}
