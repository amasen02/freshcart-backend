using FreshCart.Ordering.Domain.Primitives;

namespace FreshCart.Ordering.Domain.Orders.Events;

public sealed record OrderConfirmedDomainEvent : IDomainEvent
{
    public required Guid OrderId { get; init; }

    public required Guid CustomerId { get; init; }

    public required Money GrandTotal { get; init; }

    public required Money DiscountTotal { get; init; }

    public required Money TaxTotal { get; init; }

    public required Money ShippingTotal { get; init; }

    public required string PaymentMethod { get; init; }

    public required IReadOnlyList<OrderLine> Lines { get; init; }

    public required DateTimeOffset OccurredOnUtc { get; init; }
}
