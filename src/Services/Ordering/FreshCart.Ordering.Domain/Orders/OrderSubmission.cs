namespace FreshCart.Ordering.Domain.Orders;

/// <summary>
/// Everything required to bring a new <see cref="Order"/> into existence. Carried as a single
/// parameter object because the checkout payload is wide and every field is mandatory for the
/// factory invariants to be checked together.
/// </summary>
public sealed record OrderSubmission
{
    public required Guid OrderId { get; init; }

    public required Guid CustomerId { get; init; }

    public required string CustomerEmail { get; init; }

    public required string CustomerDisplayName { get; init; }

    public required IReadOnlyList<OrderLine> Lines { get; init; }

    public required Money Subtotal { get; init; }

    public required Money DiscountTotal { get; init; }

    public required Money TaxTotal { get; init; }

    public required Money ShippingTotal { get; init; }

    public required Money GrandTotal { get; init; }

    public required string PaymentMethod { get; init; }

    public required Address BillingAddress { get; init; }

    public Address? ShippingAddress { get; init; }

    public required DateTimeOffset SubmittedOnUtc { get; init; }
}
