using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record OrderConfirmedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required decimal GrandTotal { get; init; }
    public required decimal DiscountTotal { get; init; }
    public required decimal TaxTotal { get; init; }
    public required decimal ShippingTotal { get; init; }
    public required string CurrencyCode { get; init; }
    public required string PaymentMethod { get; init; }
    public required IReadOnlyList<OrderConfirmedLine> Lines { get; init; }
}
