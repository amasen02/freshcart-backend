using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Application.Abstractions;

/// <summary>
/// Port over the delivery store. Lookups by order id back the idempotency check (one delivery per
/// order) and the customer-owned tracking query.
/// </summary>
public interface IDeliveryRepository
{
    Task<DeliveryAggregate?> FindByIdAsync(Guid deliveryId, CancellationToken cancellationToken);

    Task<DeliveryAggregate?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(DeliveryAggregate delivery, CancellationToken cancellationToken);

    Task UpdateAsync(DeliveryAggregate delivery, CancellationToken cancellationToken);
}
