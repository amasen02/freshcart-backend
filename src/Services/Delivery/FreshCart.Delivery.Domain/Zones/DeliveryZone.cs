namespace FreshCart.Delivery.Domain.Zones;

/// <summary>
/// Geographic area served by the delivery fleet. Slots are opened per zone, and an address is
/// deliverable only when its coordinate falls inside exactly one zone polygon.
/// </summary>
public sealed class DeliveryZone
{
    private DeliveryZone(Guid id, string name, ZonePolygon polygon)
    {
        Id = id;
        Name = name;
        Polygon = polygon;
    }

    public Guid Id { get; }

    public string Name { get; }

    public ZonePolygon Polygon { get; }

    public static DeliveryZone Create(string name, ZonePolygon polygon)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(polygon);

        return new DeliveryZone(Guid.CreateVersion7(), name, polygon);
    }

    public static DeliveryZone Rehydrate(Guid id, string name, ZonePolygon polygon)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(polygon);

        return new DeliveryZone(id, name, polygon);
    }
}
