using MongoDB.Bson.Serialization.Attributes;

namespace FreshCart.Delivery.Infrastructure.Persistence.Documents;

/// <summary>
/// Persistence shape of the pending-shipment projection. The order id is the document id so the upsert
/// is naturally idempotent and the delete after scheduling is a single keyed operation.
/// </summary>
internal sealed class PendingShipmentDocument
{
    [BsonId]
    public Guid OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public AddressDocument? ShippingAddress { get; set; }

    public bool HasPhysicalLines { get; set; }
}
