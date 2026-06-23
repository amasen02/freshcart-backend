namespace FreshCart.Ordering.Application.Orders.Dtos;

public sealed record OrderLineDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string PrimaryCategory,
    decimal UnitPrice,
    int Quantity,
    bool IsDigital)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
