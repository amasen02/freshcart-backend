using System.Globalization;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Delivery.Domain.Zones;

/// <summary>
/// WGS84 point. Ranges are validated once here so every consumer of the type can trust them.
/// </summary>
public sealed record GeoCoordinate
{
    private const double MinLatitude = -90.0;
    private const double MaxLatitude = 90.0;
    private const double MinLongitude = -180.0;
    private const double MaxLongitude = 180.0;

    public GeoCoordinate(double latitude, double longitude)
    {
        if (latitude is < MinLatitude or > MaxLatitude)
        {
            throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"Latitude {latitude} is outside the WGS84 range [{MinLatitude}, {MaxLatitude}]."));
        }

        if (longitude is < MinLongitude or > MaxLongitude)
        {
            throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"Longitude {longitude} is outside the WGS84 range [{MinLongitude}, {MaxLongitude}]."));
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }
}
