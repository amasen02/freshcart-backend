using FreshCart.Delivery.Domain.Slots;

namespace FreshCart.Delivery.Application.Abstractions;

/// <summary>
/// Port over the delivery-slot store. Booking persists the incremented count through
/// <see cref="UpdateBookingAsync"/> so the slot capacity invariant survives across processes.
/// </summary>
public interface ISlotRepository
{
    Task<IReadOnlyList<DeliverySlot>> ListOpenSlotsForZoneAsync(Guid zoneId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DeliverySlot>> ListOpenSlotsOnDateAsync(DateOnly dateUtc, CancellationToken cancellationToken);

    Task UpdateBookingAsync(DeliverySlot slot, CancellationToken cancellationToken);

    Task AddAsync(DeliverySlot slot, CancellationToken cancellationToken);

    Task<bool> HasAnySlotsAsync(CancellationToken cancellationToken);
}
