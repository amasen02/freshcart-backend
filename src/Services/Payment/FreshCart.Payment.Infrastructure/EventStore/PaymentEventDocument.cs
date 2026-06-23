using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FreshCart.Payment.Infrastructure.EventStore;

/// <summary>
/// Storage envelope for one payment event. The domain payload travels as JSON text so the event
/// records stay free of driver attributes and the stored document remains human-readable during
/// an audit.
/// </summary>
public sealed class PaymentEventDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    // Driver 3.x refuses to serialize a Guid until a representation is chosen; Standard is the
    // cross-driver UUID binary subtype the unique (PaymentId, Version) index relies on.
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid PaymentId { get; set; }

    public int Version { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public DateTimeOffset OccurredOnUtc { get; set; }
}
