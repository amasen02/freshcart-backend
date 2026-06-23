using FluentAssertions;
using FreshCart.Basket.Api.Pricing;
using Xunit;

namespace FreshCart.Basket.Tests.Pricing;

public sealed class GrpcMoneyTests
{
    [Fact]
    public void WireFormatUsesInvariantCultureDecimalNotation()
    {
        GrpcMoney.ToWireFormat(1234.56m).Should().Be("1234.56");
    }

    [Fact]
    public void WireFormatRoundTripsWithoutLosingCents()
    {
        var roundTripped = GrpcMoney.Parse(GrpcMoney.ToWireFormat(19.99m));

        roundTripped.Should().Be(19.99m);
    }

    [Theory]
    [InlineData("0.1", 0.1)]
    [InlineData("100", 100)]
    [InlineData("2.505", 2.505)]
    public void ParseReadsInvariantDecimalStrings(string wireAmount, decimal expectedAmount)
    {
        GrpcMoney.Parse(wireAmount).Should().Be(expectedAmount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseRejectsBlankInput(string wireAmount)
    {
        var parsingBlankInput = () => GrpcMoney.Parse(wireAmount);

        parsingBlankInput.Should().Throw<ArgumentException>();
    }
}
