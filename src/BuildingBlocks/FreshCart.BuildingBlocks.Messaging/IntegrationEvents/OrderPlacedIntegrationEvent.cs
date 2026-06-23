using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record OrderPlacedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerEmail { get; init; }
    public required string CustomerDisplayName { get; init; }
    public required decimal GrandTotal { get; init; }
    public required string CurrencyCode { get; init; }
}
