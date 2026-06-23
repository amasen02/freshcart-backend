namespace FreshCart.Basket.Api.Domain;

/// <summary>
/// Immutable snapshot of a basket taken at checkout. Identified by the order id so support staff
/// can trace exactly what the customer checked out without bloating the live basket collection.
/// </summary>
public sealed class ArchivedBasket
{
    public required Guid Id { get; init; }

    public required Guid CustomerId { get; init; }

    public required string CurrencyCode { get; init; }

    public required IReadOnlyList<BasketItem> Items { get; init; }

    public string? CouponCode { get; init; }

    public required decimal Subtotal { get; init; }

    public required decimal DiscountTotal { get; init; }

    public required decimal TaxTotal { get; init; }

    public required decimal ShippingTotal { get; init; }

    public required decimal GrandTotal { get; init; }

    public required DateTimeOffset CheckedOutOnUtc { get; init; }
}
