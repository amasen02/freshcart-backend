namespace FreshCart.Delivery.Domain.Drivers;

/// <summary>
/// A member of the delivery fleet. The scheduling policy only ever assigns active drivers; an inactive
/// driver is retained for historical deliveries but excluded from new assignments.
/// </summary>
public sealed class Driver
{
    private Driver(Guid id, string displayName, bool isActive)
    {
        Id = id;
        DisplayName = displayName;
        IsActive = isActive;
    }

    public Guid Id { get; }

    public string DisplayName { get; }

    public bool IsActive { get; private set; }

    public static Driver Create(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new Driver(Guid.CreateVersion7(), displayName, isActive: true);
    }

    public static Driver Rehydrate(Guid id, string displayName, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new Driver(id, displayName, isActive);
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
