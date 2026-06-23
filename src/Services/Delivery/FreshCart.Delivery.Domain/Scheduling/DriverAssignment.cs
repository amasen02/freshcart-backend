namespace FreshCart.Delivery.Domain.Scheduling;

/// <summary>
/// The last moment a driver was assigned a delivery. A null timestamp means the driver has never been
/// assigned and therefore sorts ahead of anyone who has, which is how a freshly onboarded driver enters
/// the rotation immediately.
/// </summary>
public sealed record DriverAssignment(Guid DriverId, DateTimeOffset? LastAssignedOnUtc);
