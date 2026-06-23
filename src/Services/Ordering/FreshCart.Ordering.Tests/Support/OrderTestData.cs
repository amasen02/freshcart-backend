using FreshCart.Ordering.Domain.Orders;

namespace FreshCart.Ordering.Tests.Support;

/// <summary>
/// Builds valid Order aggregates and submissions for tests so each test states only what it cares
/// about. The default submission satisfies the GrandTotal invariant.
/// </summary>
public static class OrderTestData
{
    public const string CurrencyCode = "USD";

    public static readonly DateTimeOffset SubmittedOnUtc = new(2026, 6, 18, 9, 0, 0, TimeSpan.Zero);

    public static OrderSubmission ValidSubmission(Guid? orderId = null, Guid? customerId = null) => new()
    {
        OrderId = orderId ?? Guid.NewGuid(),
        CustomerId = customerId ?? Guid.NewGuid(),
        CustomerEmail = "shopper@freshcart.local",
        CustomerDisplayName = "Sample Shopper",
        PaymentMethod = "Card",
        Lines =
        [
            new OrderLine(
                Guid.NewGuid(),
                "SKU-APPLES-1KG",
                "Royal Gala Apples 1kg",
                "Produce",
                new Money(4.50m, CurrencyCode),
                2,
                IsDigital: false),
            new OrderLine(
                Guid.NewGuid(),
                "SKU-MILK-2L",
                "Full Cream Milk 2L",
                "Dairy",
                new Money(3.80m, CurrencyCode),
                1,
                IsDigital: false),
        ],
        Subtotal = new Money(12.80m, CurrencyCode),
        DiscountTotal = new Money(2.00m, CurrencyCode),
        TaxTotal = new Money(1.10m, CurrencyCode),
        ShippingTotal = new Money(5.00m, CurrencyCode),
        GrandTotal = new Money(16.90m, CurrencyCode),
        BillingAddress = new Address("12 Market Street", null, "Springfield", "12345", "US"),
        ShippingAddress = new Address("12 Market Street", "Unit 4", "Springfield", "12345", "US"),
        SubmittedOnUtc = SubmittedOnUtc,
    };

    public static Order SubmittedOrder(Guid? orderId = null, Guid? customerId = null) =>
        Order.Submit(ValidSubmission(orderId, customerId));

    public static Order StockReservedOrder(Guid reservationId)
    {
        var order = SubmittedOrder();
        order.MarkStockReserved(reservationId);
        return order;
    }

    public static Order ConfirmedOrder(Guid reservationId, Guid paymentId, DateTimeOffset confirmedOnUtc)
    {
        var order = StockReservedOrder(reservationId);
        order.MarkPaid(paymentId);
        order.Confirm(confirmedOnUtc);
        return order;
    }
}
