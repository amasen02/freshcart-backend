using FreshCart.Delivery.Domain.Zones;

namespace FreshCart.Delivery.Application.Abstractions;

/// <summary>
/// Port over the zone store. The point-in-polygon match is delegated to the adapter because that is a
/// geospatial-query concern (a 2dsphere <c>$geoIntersects</c> in the Mongo adapter), not a domain rule.
/// </summary>
public interface IZoneRepository
{
    Task<DeliveryZone?> FindZoneContainingAsync(GeoCoordinate coordinate, CancellationToken cancellationToken);

    Task<IReadOnlyList<DeliveryZone>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(DeliveryZone zone, CancellationToken cancellationToken);
}
