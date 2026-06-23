using FreshCart.Delivery.Domain.Slots;

namespace FreshCart.Delivery.Domain.Scheduling;

/// <summary>
/// The slot and driver the <see cref="DeliverySchedulingPolicy"/> selected for a delivery. The chosen
/// slot is returned by reference so the caller books it and persists the increment atomically.
/// </summary>
public sealed record ScheduleProposal(DeliverySlot Slot, Guid DriverId);
