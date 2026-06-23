using System.Text;
using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Domain.Zones;

namespace FreshCart.Delivery.Infrastructure.Geocoding;

/// <summary>
/// Honest stand-in for the geocoding port: it hashes the normalised postal code into a coordinate that
/// always falls inside the seeded city bounds, so the same postcode always resolves to the same point.
/// A production deployment replaces this with an Azure Maps or Google adapter behind the identical
/// <see cref="IGeocodingService"/> port; that the swap touches nothing else is the whole reason the
/// port exists. The hash is a fixed FNV-1a, not <see cref="string.GetHashCode()"/>, because the runtime
/// hash is randomised per process and would break determinism across restarts.
/// </summary>
public sealed class DeterministicGeocodingAdapter : IGeocodingService
{
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;
    private const int LatitudeHashShift = 16;
    private const uint LowSixteenBitsMask = 0xFFFF;
    private const double SixteenBitScale = 65535.0;

    public GeoCoordinate Locate(DeliveryAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        var seed = NormalisePostalCode(address.PostalCode);
        var hash = ComputeFnv1aHash(seed);

        var latitudeFraction = ((hash >> LatitudeHashShift) & LowSixteenBitsMask) / SixteenBitScale;
        var longitudeFraction = (hash & LowSixteenBitsMask) / SixteenBitScale;

        var latitude = SeededCityBounds.MinLatitude + (latitudeFraction * SeededCityBounds.LatitudeSpan);
        var longitude = SeededCityBounds.MinLongitude + (longitudeFraction * SeededCityBounds.LongitudeSpan);

        return new GeoCoordinate(latitude, longitude);
    }

    private static string NormalisePostalCode(string postalCode) =>
        postalCode.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    private static uint ComputeFnv1aHash(string value)
    {
        var hash = FnvOffsetBasis;
        foreach (var octet in Encoding.UTF8.GetBytes(value))
        {
            hash ^= octet;
            hash *= FnvPrime;
        }

        return hash;
    }
}
