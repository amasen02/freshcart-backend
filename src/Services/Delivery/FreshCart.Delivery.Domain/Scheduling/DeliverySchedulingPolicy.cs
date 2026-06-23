using FreshCart.Delivery.Domain.Slots;

namespace FreshCart.Delivery.Domain.Scheduling;

/// <summary>
/// Pure scheduling rule with no I/O: given the open slots for a matched zone and the current driver
/// rotation, it selects the earliest slot that still has free capacity and the least-recently-assigned
/// active driver. Keeping this a function of its inputs is the point of the hexagon: the rule is fully
/// unit-testable and the adapters supply the data it reasons over.
/// </summary>
public static class DeliverySchedulingPolicy
{
    /// <summary>
    /// Returns the slot and driver to book, or <see langword="null"/> when no slot has free capacity or
    /// no active driver exists. The caller treats a null proposal as "cannot schedule right now".
    /// </summary>
    public static ScheduleProposal? Propose(
        IReadOnlyCollection<DeliverySlot> zoneSlots,
        IReadOnlyCollection<DriverAssignment> activeDriverRotation)
    {
        ArgumentNullException.ThrowIfNull(zoneSlots);
        ArgumentNullException.ThrowIfNull(activeDriverRotation);

        var earliestOpenSlot = zoneSlots
            .Where(slot => slot.HasFreeCapacity)
            .OrderBy(slot => slot.StartUtc)
            .ThenBy(slot => slot.Id)
            .FirstOrDefault();

        if (earliestOpenSlot is null)
        {
            return null;
        }

        var nextDriver = activeDriverRotation
            .OrderBy(assignment => assignment.LastAssignedOnUtc ?? DateTimeOffset.MinValue)
            .ThenBy(assignment => assignment.DriverId)
            .FirstOrDefault();

        return nextDriver is null
            ? null
            : new ScheduleProposal(earliestOpenSlot, nextDriver.DriverId);
    }
}
