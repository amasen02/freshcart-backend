namespace FreshCart.Delivery.Application.Tracking;

/// <summary>
/// A bookable slot offered to the customer, with the remaining capacity so the SPA can show how many
/// deliveries the window can still take.
/// </summary>
public sealed record OpenSlotDto(
    Guid SlotId,
    Guid ZoneId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int RemainingCapacity);
