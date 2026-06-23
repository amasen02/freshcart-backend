using MongoDB.Bson.Serialization.Attributes;

namespace FreshCart.Delivery.Infrastructure.Persistence.Documents;

/// <summary>
/// Persistence shape of a delivery. Decoupled from the domain aggregate so the storage schema can carry
/// adapter concerns (BSON ids) without those leaking into the hexagon. GUIDs serialise in the standard
/// representation through the process-wide serializer registered in
/// <see cref="MongoSerializationConfiguration"/>, so no per-member representation attribute is needed.
/// </summary>
internal sealed class DeliveryDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public required AddressDocument Address { get; set; }

    public int Status { get; set; }

    public DateTimeOffset SlotStartUtc { get; set; }

    public DateTimeOffset SlotEndUtc { get; set; }

    public Guid? DriverId { get; set; }

    public DateTimeOffset CreatedOnUtc { get; set; }

    public DateTimeOffset? CompletedOnUtc { get; set; }
}
