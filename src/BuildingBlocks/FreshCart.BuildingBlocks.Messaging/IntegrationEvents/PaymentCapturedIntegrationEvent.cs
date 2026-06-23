using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record PaymentCapturedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid PaymentId { get; init; }
    public required decimal Amount { get; init; }
    public required string CurrencyCode { get; init; }
    public required string PaymentMethod { get; init; }
}
