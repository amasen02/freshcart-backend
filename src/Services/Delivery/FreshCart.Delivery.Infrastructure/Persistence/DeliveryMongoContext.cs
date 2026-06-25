using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.Delivery.Infrastructure.Persistence.Documents;
using MongoDB.Driver;

namespace FreshCart.Delivery.Infrastructure.Persistence;

/// <summary>
/// Single point of access to the delivery collections. Centralising the collection names and the
/// <see cref="IMongoDatabase"/> here keeps the repositories free of stringly-typed collection lookups.
/// </summary>
public sealed class DeliveryMongoContext
{
    internal const string DeliveriesCollectionName = "deliveries";
    internal const string SlotsCollectionName = "slots";
    internal const string ZonesCollectionName = "zones";
    internal const string DriversCollectionName = "drivers";
    internal const string PendingShipmentsCollectionName = "pending-shipments";
    internal const string OutboxCollectionName = "delivery-outbox";

    private readonly IMongoDatabase database;

    public DeliveryMongoContext(IMongoClient mongoClient, DeliveryMongoOptions options)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);

        database = mongoClient.GetDatabase(options.DatabaseName);
    }

    internal IMongoCollection<DeliveryDocument> Deliveries =>
        database.GetCollection<DeliveryDocument>(DeliveriesCollectionName);

    internal IMongoCollection<SlotDocument> Slots =>
        database.GetCollection<SlotDocument>(SlotsCollectionName);

    internal IMongoCollection<ZoneDocument> Zones =>
        database.GetCollection<ZoneDocument>(ZonesCollectionName);

    internal IMongoCollection<DriverDocument> Drivers =>
        database.GetCollection<DriverDocument>(DriversCollectionName);

    internal IMongoCollection<PendingShipmentDocument> PendingShipments =>
        database.GetCollection<PendingShipmentDocument>(PendingShipmentsCollectionName);

    internal IMongoCollection<OutboxMessage> Outbox =>
        database.GetCollection<OutboxMessage>(OutboxCollectionName);
}
