using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record StockReservedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid ReservationId { get; init; }
}
