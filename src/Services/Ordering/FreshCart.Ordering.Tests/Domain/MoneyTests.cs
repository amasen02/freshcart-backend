using FluentAssertions;
using FreshCart.Ordering.Domain.Exceptions;
using FreshCart.Ordering.Domain.Orders;

namespace FreshCart.Ordering.Tests.Domain;

public sealed class MoneyTests
{
    private const string Usd = "USD";
    private const string Eur = "EUR";

    [Fact]
    public void AddSumsAmountsWhenCurrenciesMatch()
    {
        var result = new Money(10.00m, Usd).Add(new Money(2.50m, Usd));

        result.Amount.Should().Be(12.50m);
        result.CurrencyCode.Should().Be(Usd);
    }

    [Fact]
    public void SubtractReducesAmountWhenCurrenciesMatch()
    {
        var result = new Money(10.00m, Usd).Subtract(new Money(2.50m, Usd));

        result.Amount.Should().Be(7.50m);
        result.CurrencyCode.Should().Be(Usd);
    }

    [Fact]
    public void MultiplyByScalesAmountByQuantity()
    {
        var result = new Money(4.50m, Usd).MultiplyBy(3);

        result.Amount.Should().Be(13.50m);
        result.CurrencyCode.Should().Be(Usd);
    }

    [Fact]
    public void CurrencyComparisonIsCaseInsensitiveSoLowercaseCodesStillAdd()
    {
        var result = new Money(1.00m, "usd").Add(new Money(1.00m, Usd));

        result.Amount.Should().Be(2.00m);
    }

    [Fact]
    public void AddThrowsWhenCurrenciesDiffer()
    {
        var act = () => new Money(10.00m, Usd).Add(new Money(1.00m, Eur));

        act.Should().Throw<OrderDomainException>().WithMessage("*EUR*USD*");
    }

    [Fact]
    public void SubtractThrowsWhenCurrenciesDiffer()
    {
        var act = () => new Money(10.00m, Usd).Subtract(new Money(1.00m, Eur));

        act.Should().Throw<OrderDomainException>();
    }
}
