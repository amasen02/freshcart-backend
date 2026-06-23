using MongoDB.Bson.Serialization.Attributes;

namespace FreshCart.Delivery.Infrastructure.Persistence.Documents;

/// <summary>
/// Persistence shape of a delivery slot.
/// </summary>
internal sealed class SlotDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid ZoneId { get; set; }

    public DateTimeOffset StartUtc { get; set; }

    public DateTimeOffset EndUtc { get; set; }

    public int Capacity { get; set; }

    public int BookedCount { get; set; }
}
