using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace FreshCart.Delivery.Infrastructure.Persistence.Documents;

/// <summary>
/// Persistence shape of a delivery zone. The polygon is stored as native GeoJSON so the 2dsphere index
/// can answer point-in-polygon queries with <c>$geoIntersects</c> in the database rather than pulling
/// every zone into memory.
/// </summary>
internal sealed class ZoneDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required GeoJsonPolygon<GeoJson2DGeographicCoordinates> Boundary { get; set; }
}
