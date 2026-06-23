using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record PaymentFailedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public Guid? PaymentId { get; init; }
    public required string Reason { get; init; }
}
