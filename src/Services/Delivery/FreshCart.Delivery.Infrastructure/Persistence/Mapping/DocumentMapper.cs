using FreshCart.Delivery.Application.Shipments;
using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Domain.Drivers;
using FreshCart.Delivery.Domain.Slots;
using FreshCart.Delivery.Domain.Zones;
using FreshCart.Delivery.Infrastructure.Persistence.Documents;
using MongoDB.Driver.GeoJsonObjectModel;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Infrastructure.Persistence.Mapping;

/// <summary>
/// Translates between the persistence documents and the domain types. Keeping the translation in one
/// place is what lets every other adapter speak pure domain while the storage schema stays an
/// infrastructure detail. GeoJSON longitude precedes latitude by the spec; the domain coordinate is the
/// reverse, so the ordering is converted here once.
/// </summary>
internal static class DocumentMapper
{
    public static DeliveryDocument ToDocument(DeliveryAggregate delivery) => new()
    {
        Id = delivery.Id,
        OrderId = delivery.OrderId,
        CustomerId = delivery.CustomerId,
        Address = ToAddressDocument(delivery.Address),
        Status = (int)delivery.Status,
        SlotStartUtc = delivery.SlotStartUtc,
        SlotEndUtc = delivery.SlotEndUtc,
        DriverId = delivery.DriverId,
        CreatedOnUtc = delivery.CreatedOnUtc,
        CompletedOnUtc = delivery.CompletedOnUtc,
    };

    public static SlotDocument ToDocument(DeliverySlot slot) => new()
    {
        Id = slot.Id,
        ZoneId = slot.ZoneId,
        StartUtc = slot.StartUtc,
        EndUtc = slot.EndUtc,
        Capacity = slot.Capacity,
        BookedCount = slot.BookedCount,
    };

    public static DriverDocument ToDocument(Driver driver) => new()
    {
        Id = driver.Id,
        DisplayName = driver.DisplayName,
        IsActive = driver.IsActive,
    };

    public static ZoneDocument ToDocument(DeliveryZone zone) => new()
    {
        Id = zone.Id,
        Name = zone.Name,
        Boundary = ToGeoJsonPolygon(zone.Polygon),
    };

    public static PendingShipmentDocument ToDocument(PendingShipment pendingShipment) => new()
    {
        OrderId = pendingShipment.OrderId,
        CustomerId = pendingShipment.CustomerId,
        ShippingAddress = pendingShipment.ShippingAddress is null
            ? null
            : ToAddressDocument(pendingShipment.ShippingAddress),
        HasPhysicalLines = pendingShipment.HasPhysicalLines,
    };

    public static DeliveryAggregate ToDomain(DeliveryDocument document) => DeliveryAggregate.Rehydrate(
        document.Id,
        document.OrderId,
        document.CustomerId,
        ToDomainAddress(document.Address),
        (DeliveryStatus)document.Status,
        document.SlotStartUtc,
        document.SlotEndUtc,
        document.DriverId,
        document.CreatedOnUtc,
        document.CompletedOnUtc);

    public static DeliverySlot ToDomain(SlotDocument document) => DeliverySlot.Rehydrate(
        document.Id,
        document.ZoneId,
        document.StartUtc,
        document.EndUtc,
        document.Capacity,
        document.BookedCount);

    public static Driver ToDomain(DriverDocument document) =>
        Driver.Rehydrate(document.Id, document.DisplayName, document.IsActive);

    public static DeliveryZone ToDomain(ZoneDocument document) =>
        DeliveryZone.Rehydrate(document.Id, document.Name, ToDomainPolygon(document.Boundary));

    public static PendingShipment ToDomain(PendingShipmentDocument document) => new(
        document.OrderId,
        document.CustomerId,
        document.ShippingAddress is null ? null : ToDomainAddress(document.ShippingAddress),
        document.HasPhysicalLines);

    public static GeoJsonPoint<GeoJson2DGeographicCoordinates> ToGeoJsonPoint(GeoCoordinate coordinate) =>
        GeoJson.Point(new GeoJson2DGeographicCoordinates(coordinate.Longitude, coordinate.Latitude));

    private static AddressDocument ToAddressDocument(DeliveryAddress address) => new()
    {
        Line1 = address.Line1,
        Line2 = address.Line2,
        City = address.City,
        PostalCode = address.PostalCode,
        CountryCode = address.CountryCode,
    };

    private static DeliveryAddress ToDomainAddress(AddressDocument document) => new(
        document.Line1,
        document.Line2,
        document.City,
        document.PostalCode,
        document.CountryCode);

    private static GeoJsonPolygon<GeoJson2DGeographicCoordinates> ToGeoJsonPolygon(ZonePolygon polygon)
    {
        var coordinates = polygon.ExteriorRing
            .Select(point => new GeoJson2DGeographicCoordinates(point.Longitude, point.Latitude))
            .ToArray();

        return GeoJson.Polygon(coordinates);
    }

    private static ZonePolygon ToDomainPolygon(GeoJsonPolygon<GeoJson2DGeographicCoordinates> boundary)
    {
        var ring = boundary.Coordinates.Exterior.Positions
            .Select(position => new GeoCoordinate(position.Latitude, position.Longitude))
            .ToList();

        return new ZonePolygon(ring);
    }
}
