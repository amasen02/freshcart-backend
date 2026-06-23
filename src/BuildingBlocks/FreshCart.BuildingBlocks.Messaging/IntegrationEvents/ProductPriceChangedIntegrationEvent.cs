using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record ProductPriceChangedIntegrationEvent : IntegrationEvent
{
    public required Guid ProductId { get; init; }
    public required string ProductSku { get; init; }
    public required decimal OldPrice { get; init; }
    public required decimal NewPrice { get; init; }
}
