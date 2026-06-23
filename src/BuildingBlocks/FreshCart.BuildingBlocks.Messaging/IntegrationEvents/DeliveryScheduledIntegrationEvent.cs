using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record DeliveryScheduledIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid DeliveryId { get; init; }
    public required Guid CustomerId { get; init; }
    public required DateTimeOffset SlotStartUtc { get; init; }
    public required DateTimeOffset SlotEndUtc { get; init; }
}
