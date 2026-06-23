using FreshCart.Delivery.Domain.Zones;

namespace FreshCart.Delivery.Infrastructure.Seeding;

/// <summary>
/// The fixed Development seed: two adjacent zone polygons tiling the city box, the fleet roster, and the
/// slot grid parameters. Kept as data, separate from the seeding mechanism, so the geography and the
/// fleet can be reviewed at a glance and the seeder stays a simple loop.
/// </summary>
internal static class DevelopmentSeedData
{
    public const int SlotDaysAhead = 7;
    public const int SlotsPerDayPerZone = 3;
    public const int SlotCapacity = 5;

    public const int FirstSlotStartHourUtc = 9;
    public const int SlotDurationHours = 3;

    public static IReadOnlyList<string> DriverNames { get; } =
    [
        "Amara Perera",
        "Devin Cole",
        "Femi Adeyemi",
        "Noor Haddad",
    ];

    // The two zones tile the geocoder's city box exactly (shared edge at latitude 51.510), so any
    // deterministically geocoded address resolves to exactly one serving zone in Development.
    public static IReadOnlyList<SeedZoneDefinition> Zones { get; } =
    [
        new SeedZoneDefinition("City Centre", BuildRectangle(51.510, 51.540, -0.150, -0.040)),
        new SeedZoneDefinition("Suburbs", BuildRectangle(51.480, 51.510, -0.150, -0.040)),
    ];

    private static ZonePolygon BuildRectangle(
        double minLatitude,
        double maxLatitude,
        double minLongitude,
        double maxLongitude)
    {
        var bottomLeft = new GeoCoordinate(minLatitude, minLongitude);

        return new ZonePolygon(
        [
            bottomLeft,
            new GeoCoordinate(minLatitude, maxLongitude),
            new GeoCoordinate(maxLatitude, maxLongitude),
            new GeoCoordinate(maxLatitude, minLongitude),
            bottomLeft,
        ]);
    }
}
