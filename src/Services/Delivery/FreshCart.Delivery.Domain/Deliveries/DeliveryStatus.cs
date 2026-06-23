namespace FreshCart.Delivery.Domain.Deliveries;

/// <summary>
/// Lifecycle of a physical delivery. Transitions are linear: a scheduled delivery goes out for
/// delivery, then either completes or fails. The aggregate guards every transition.
/// </summary>
public enum DeliveryStatus
{
    Scheduled = 0,
    OutForDelivery = 1,
    Completed = 2,
    Failed = 3,
}
