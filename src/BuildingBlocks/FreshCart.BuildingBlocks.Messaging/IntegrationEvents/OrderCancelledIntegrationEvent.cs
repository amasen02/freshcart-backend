using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record OrderCancelledIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string Reason { get; init; }
}
