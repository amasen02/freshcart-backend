using FreshCart.Delivery.Domain.Deliveries;

namespace FreshCart.Delivery.Application.Tracking;

/// <summary>
/// Customer-facing tracking view of a delivery. Carries the slot window, current status and assigned
/// driver without exposing the internal aggregate.
/// </summary>
public sealed record DeliveryTrackingDto(
    Guid DeliveryId,
    Guid OrderId,
    Guid CustomerId,
    DeliveryStatus Status,
    DeliveryAddressDto Address,
    DateTimeOffset SlotStartUtc,
    DateTimeOffset SlotEndUtc,
    Guid? DriverId,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? CompletedOnUtc);
