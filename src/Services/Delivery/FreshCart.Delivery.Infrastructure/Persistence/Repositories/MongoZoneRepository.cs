using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Domain.Zones;
using FreshCart.Delivery.Infrastructure.Persistence.Documents;
using FreshCart.Delivery.Infrastructure.Persistence.Mapping;
using MongoDB.Driver;

namespace FreshCart.Delivery.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB adapter for <see cref="IZoneRepository"/>. The containment check is a server-side
/// <c>$geoIntersects</c> against the 2dsphere index on the zone boundary, so resolving the serving zone
/// for an address is a single indexed query no matter how many zones exist.
/// </summary>
public sealed class MongoZoneRepository(DeliveryMongoContext context) : IZoneRepository
{
    public async Task<DeliveryZone?> FindZoneContainingAsync(
        GeoCoordinate coordinate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(coordinate);

        var point = DocumentMapper.ToGeoJsonPoint(coordinate);
        var filter = Builders<ZoneDocument>.Filter.GeoIntersects(zone => zone.Boundary, point);

        var document = await context.Zones
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document is null ? null : DocumentMapper.ToDomain(document);
    }

    public async Task<IReadOnlyList<DeliveryZone>> ListAsync(CancellationToken cancellationToken)
    {
        var documents = await context.Zones
            .Find(Builders<ZoneDocument>.Filter.Empty)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return documents.Select(DocumentMapper.ToDomain).ToList();
    }

    public Task AddAsync(DeliveryZone zone, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(zone);

        return context.Zones
            .InsertOneAsync(DocumentMapper.ToDocument(zone), options: null, cancellationToken);
    }
}
