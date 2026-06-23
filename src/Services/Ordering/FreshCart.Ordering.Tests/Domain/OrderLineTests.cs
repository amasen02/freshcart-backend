using FluentAssertions;
using FreshCart.Ordering.Domain.Orders;

namespace FreshCart.Ordering.Tests.Domain;

public sealed class OrderLineTests
{
    [Fact]
    public void LineTotalMultipliesUnitPriceByQuantity()
    {
        var line = new OrderLine(
            Guid.NewGuid(),
            "SKU-APPLES-1KG",
            "Royal Gala Apples 1kg",
            "Produce",
            new Money(4.50m, "USD"),
            3,
            IsDigital: false);

        line.LineTotal.Amount.Should().Be(13.50m);
        line.LineTotal.CurrencyCode.Should().Be("USD");
    }
}
