namespace FreshCart.Ordering.Domain.Orders;

public sealed record OrderLine(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string PrimaryCategory,
    Money UnitPrice,
    int Quantity,
    bool IsDigital)
{
    // EF Core cannot bind the owned UnitPrice navigation through the positional constructor.
    private OrderLine()
        : this(Guid.Empty, string.Empty, string.Empty, string.Empty, null!, 0, false)
    {
    }

    public Money LineTotal => UnitPrice.MultiplyBy(Quantity);
}
