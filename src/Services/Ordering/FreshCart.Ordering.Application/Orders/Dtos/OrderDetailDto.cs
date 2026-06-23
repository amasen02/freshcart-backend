namespace FreshCart.Ordering.Application.Orders.Dtos;

public sealed record OrderDetailDto
{
    public required Guid OrderId { get; init; }

    public required Guid CustomerId { get; init; }

    public required string Status { get; init; }

    public required string CustomerEmail { get; init; }

    public required string CustomerDisplayName { get; init; }

    public required string PaymentMethod { get; init; }

    public required decimal Subtotal { get; init; }

    public required decimal DiscountTotal { get; init; }

    public required decimal TaxTotal { get; init; }

    public required decimal ShippingTotal { get; init; }

    public required decimal GrandTotal { get; init; }

    public required string CurrencyCode { get; init; }

    public required OrderAddressDto BillingAddress { get; init; }

    public OrderAddressDto? ShippingAddress { get; init; }

    public string? FailureReason { get; init; }

    public required DateTimeOffset SubmittedOnUtc { get; init; }

    public DateTimeOffset? ConfirmedOnUtc { get; init; }

    public DateTimeOffset? CancelledOnUtc { get; init; }

    public required IReadOnlyList<OrderLineDto> Lines { get; init; }
}
