using FreshCart.BuildingBlocks.Messaging.Events;

namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record OrderRefundedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required decimal RefundAmount { get; init; }
    public required string CurrencyCode { get; init; }
    public required string Reason { get; init; }
}
