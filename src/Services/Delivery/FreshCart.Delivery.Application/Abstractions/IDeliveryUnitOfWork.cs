using FreshCart.BuildingBlocks.Messaging.Events;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Application.Abstractions;

/// <summary>
/// Transactional writer that persists a delivery and stages the integration event it raises in one
/// atomic unit. Closing the dual-write gap is the point: publishing the event directly after the write
/// (the previous behaviour) lost the event whenever the broker call failed after the document committed.
/// The event is staged in the outbox here and a background publisher delivers it; consumers are
/// idempotent so the resulting at-least-once delivery is safe.
/// </summary>
public interface IDeliveryUnitOfWork
{
    Task PersistScheduledDeliveryAsync(
        DeliveryAggregate delivery,
        IntegrationEvent scheduledEvent,
        CancellationToken cancellationToken);

    Task PersistCompletedDeliveryAsync(
        DeliveryAggregate delivery,
        IntegrationEvent completedEvent,
        CancellationToken cancellationToken);
}
