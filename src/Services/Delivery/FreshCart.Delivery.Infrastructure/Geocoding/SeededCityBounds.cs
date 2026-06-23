namespace FreshCart.Delivery.Infrastructure.Geocoding;

/// <summary>
/// The latitude/longitude rectangle the deterministic geocoder maps postal codes into. It is sized to
/// contain both seeded zone polygons so a hashed coordinate always lands in a served area, which keeps
/// the DEV stand-in honest end to end. Production swaps in a real maps adapter and ignores this box.
/// </summary>
internal static class SeededCityBounds
{
    public const double MinLatitude = 51.480;
    public const double MaxLatitude = 51.540;
    public const double MinLongitude = -0.150;
    public const double MaxLongitude = -0.040;

    public const double LatitudeSpan = MaxLatitude - MinLatitude;
    public const double LongitudeSpan = MaxLongitude - MinLongitude;
}
