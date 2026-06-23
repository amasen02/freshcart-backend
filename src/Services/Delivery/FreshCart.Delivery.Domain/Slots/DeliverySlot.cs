using System.Globalization;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Delivery.Domain.Slots;

/// <summary>
/// A bookable delivery window for a single zone. Capacity is the hard ceiling on concurrent deliveries
/// the fleet can serve in the window; the <see cref="Book"/> invariant guarantees the fleet is never
/// oversubscribed regardless of how many schedulers race for the same slot.
/// </summary>
public sealed class DeliverySlot
{
    private DeliverySlot(
        Guid id,
        Guid zoneId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        int capacity,
        int bookedCount)
    {
        if (endUtc <= startUtc)
        {
            throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"A delivery slot must end after it starts; received start {startUtc:O} and end {endUtc:O}."));
        }

        if (capacity <= 0)
        {
            throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"A delivery slot capacity must be positive; received {capacity}."));
        }

        if (bookedCount < 0 || bookedCount > capacity)
        {
            throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"A delivery slot booked count must be within [0, {capacity}]; received {bookedCount}."));
        }

        Id = id;
        ZoneId = zoneId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Capacity = capacity;
        BookedCount = bookedCount;
    }

    public Guid Id { get; }

    public Guid ZoneId { get; }

    public DateTimeOffset StartUtc { get; }

    public DateTimeOffset EndUtc { get; }

    public int Capacity { get; }

    public int BookedCount { get; private set; }

    public bool HasFreeCapacity => BookedCount < Capacity;

    public static DeliverySlot Create(Guid zoneId, DateTimeOffset startUtc, DateTimeOffset endUtc, int capacity)
        => new(Guid.CreateVersion7(), zoneId, startUtc, endUtc, capacity, bookedCount: 0);

    public static DeliverySlot Rehydrate(
        Guid id,
        Guid zoneId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        int capacity,
        int bookedCount)
        => new(id, zoneId, startUtc, endUtc, capacity, bookedCount);

    /// <summary>
    /// Reserves one unit of capacity. Throws when the slot is already full so a caller can never push
    /// the fleet past what it can serve in the window.
    /// </summary>
    public void Book()
    {
        if (!HasFreeCapacity)
        {
            throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"Delivery slot {Id} is full: {BookedCount} of {Capacity} bookings are taken."));
        }

        BookedCount++;
    }
}
