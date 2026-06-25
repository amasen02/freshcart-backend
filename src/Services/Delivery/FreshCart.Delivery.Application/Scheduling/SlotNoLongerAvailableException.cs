namespace FreshCart.Delivery.Application.Scheduling;

/// <summary>
/// Raised when the slot the scheduling policy proposed was taken to capacity by a concurrent scheduler
/// between listing it and booking it. The loss is decided server-side by the conditional capacity update,
/// so this is an expected race rather than a fault: the consumer lets it escape so the MassTransit retry
/// policy re-runs the use case, which re-lists slots with the now-full one excluded and books the next
/// open window.
/// </summary>
public sealed class SlotNoLongerAvailableException : Exception
{
    public SlotNoLongerAvailableException(Guid orderId, Guid slotId)
        : base($"Delivery slot \"{slotId}\" filled to capacity before order \"{orderId}\" could book it; the OrderConfirmed event will be retried.")
    {
        OrderId = orderId;
        SlotId = slotId;
    }

    public Guid OrderId { get; }

    public Guid SlotId { get; }
}
