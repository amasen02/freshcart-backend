using FreshCart.Delivery.Domain.Slots;

namespace FreshCart.Delivery.Application.Abstractions;

/// <summary>
/// Port over the delivery-slot store. Booking goes through <see cref="TryBookSlotAsync"/>, which reserves
/// one unit of capacity atomically so the slot's capacity invariant survives schedulers racing across
/// processes — read-then-write would lose updates.
/// </summary>
public interface ISlotRepository
{
    Task<IReadOnlyList<DeliverySlot>> ListOpenSlotsForZoneAsync(Guid zoneId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DeliverySlot>> ListOpenSlotsOnDateAsync(DateOnly dateUtc, CancellationToken cancellationToken);

    Task<bool> TryBookSlotAsync(DeliverySlot slot, CancellationToken cancellationToken);

    Task AddAsync(DeliverySlot slot, CancellationToken cancellationToken);

    Task<bool> HasAnySlotsAsync(CancellationToken cancellationToken);
}
