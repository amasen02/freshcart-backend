using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record BasketCheckoutStartedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerEmail { get; init; }
    public required string CustomerDisplayName { get; init; }
    public required string CurrencyCode { get; init; }
    public required string PaymentMethod { get; init; }
    public string? CouponCode { get; init; }
    public required decimal Subtotal { get; init; }
    public required decimal DiscountTotal { get; init; }
    public required decimal TaxTotal { get; init; }
    public required decimal ShippingTotal { get; init; }
    public required decimal GrandTotal { get; init; }
    public required CheckoutAddress BillingAddress { get; init; }
    public CheckoutAddress? ShippingAddress { get; init; }
    public required IReadOnlyList<CheckoutLine> Lines { get; init; }
}
