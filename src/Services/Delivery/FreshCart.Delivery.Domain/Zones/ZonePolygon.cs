using System.Globalization;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Delivery.Domain.Zones;

/// <summary>
/// Closed exterior ring describing the area a zone serves. Kept as a plain coordinate list so the
/// domain stays free of any geo library; adapters translate it to their native polygon type.
/// </summary>
public sealed record ZonePolygon
{
    private const int MinimumClosedRingPointCount = 4;

    public ZonePolygon(IReadOnlyList<GeoCoordinate> exteriorRing)
    {
        ArgumentNullException.ThrowIfNull(exteriorRing);

        if (exteriorRing.Count < MinimumClosedRingPointCount)
        {
            throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"A zone polygon needs at least {MinimumClosedRingPointCount} coordinates to form a closed ring; {exteriorRing.Count} were provided."));
        }

        if (exteriorRing[0] != exteriorRing[^1])
        {
            throw new DomainException("A zone polygon exterior ring must be closed: the first and last coordinates must be equal.");
        }

        ExteriorRing = exteriorRing;
    }

    public IReadOnlyList<GeoCoordinate> ExteriorRing { get; }
}
