namespace FreshCart.Ordering.Domain.Orders;

public enum OrderStatus
{
    Submitted,
    StockReserved,
    Paid,
    Confirmed,
    Cancelled,
    Refunded,
}
