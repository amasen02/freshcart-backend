using FreshCart.Delivery.Application.Shipments;

namespace FreshCart.Delivery.Application.Abstractions;

/// <summary>
/// Port over the pending-shipment projection. Upserts are idempotent so a redelivered
/// <c>BasketCheckoutStartedIntegrationEvent</c> overwrites rather than duplicates, and the row is
/// deleted once scheduling has consumed it.
/// </summary>
public interface IPendingShipmentRepository
{
    Task UpsertAsync(PendingShipment pendingShipment, CancellationToken cancellationToken);

    Task<PendingShipment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task DeleteByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
}
