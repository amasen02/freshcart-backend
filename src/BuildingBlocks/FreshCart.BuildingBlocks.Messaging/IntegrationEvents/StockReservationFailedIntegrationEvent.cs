using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record StockReservationFailedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required string Reason { get; init; }
    public required IReadOnlyList<string> UnavailableSkus { get; init; }
}
