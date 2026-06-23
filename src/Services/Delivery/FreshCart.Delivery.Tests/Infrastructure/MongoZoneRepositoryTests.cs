using FluentAssertions;
using FreshCart.Delivery.Domain.Zones;
using FreshCart.Delivery.Infrastructure.Persistence.Repositories;

namespace FreshCart.Delivery.Tests.Infrastructure;

[Collection(MongoIntegrationCollection.Name)]
public sealed class MongoZoneRepositoryTests(MongoIntegrationFixture fixture)
{
    private readonly MongoZoneRepository repository = new(fixture.Context);

    [Fact]
    public async Task FindsTheZoneWhosePolygonContainsThePoint()
    {
        var cityCentre = DeliveryZone.Create(UniqueZoneName(), Rectangle(51.50, 51.54, -0.15, -0.08));
        var suburbs = DeliveryZone.Create(UniqueZoneName(), Rectangle(51.40, 51.45, 0.05, 0.10));

        await repository.AddAsync(cityCentre, CancellationToken.None);
        await repository.AddAsync(suburbs, CancellationToken.None);

        var insideCityCentre = new GeoCoordinate(51.52, -0.11);
        var match = await repository.FindZoneContainingAsync(insideCityCentre, CancellationToken.None);

        match.Should().NotBeNull();
        match!.Id.Should().Be(cityCentre.Id);
        match.Name.Should().Be(cityCentre.Name);
    }

    [Fact]
    public async Task RoundTripsThePolygonRingThroughGeoJsonAndBack()
    {
        var polygon = Rectangle(40.70, 40.80, -74.02, -73.95);
        var zone = DeliveryZone.Create(UniqueZoneName(), polygon);
        await repository.AddAsync(zone, CancellationToken.None);

        var match = await repository.FindZoneContainingAsync(new GeoCoordinate(40.75, -73.98), CancellationToken.None);

        match.Should().NotBeNull();
        match!.Polygon.ExteriorRing.Should().HaveCount(polygon.ExteriorRing.Count);
        match.Polygon.ExteriorRing[0].Latitude.Should().BeApproximately(polygon.ExteriorRing[0].Latitude, 1e-9);
        match.Polygon.ExteriorRing[0].Longitude.Should().BeApproximately(polygon.ExteriorRing[0].Longitude, 1e-9);
    }

    [Fact]
    public async Task ReturnsNullWhenThePointFallsOutsideEveryZone()
    {
        var zone = DeliveryZone.Create(UniqueZoneName(), Rectangle(10.0, 10.1, 10.0, 10.1));
        await repository.AddAsync(zone, CancellationToken.None);

        var farAway = new GeoCoordinate(-33.87, 151.21);
        var match = await repository.FindZoneContainingAsync(farAway, CancellationToken.None);

        match.Should().BeNull();
    }

    private static string UniqueZoneName() => $"zone-{Guid.NewGuid():N}";

    private static ZonePolygon Rectangle(
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
