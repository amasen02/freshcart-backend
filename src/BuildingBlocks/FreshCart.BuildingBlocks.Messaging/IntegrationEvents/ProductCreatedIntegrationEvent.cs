using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record ProductCreatedIntegrationEvent : IntegrationEvent
{
    public required Guid ProductId { get; init; }
    public required string ProductSku { get; init; }
    public required string ProductName { get; init; }
    public required string PrimaryCategory { get; init; }
    public required decimal BasePrice { get; init; }
    public required string CurrencyCode { get; init; }
    public required int InitialStockQuantity { get; init; }
    public required bool IsDigital { get; init; }
}
