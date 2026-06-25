using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FreshCart.Payment.Infrastructure.Projections;

/// <summary>
/// A durable "this stream needs projecting" marker, written in the same MongoDB transaction as the events
/// it follows. It is what makes the SQL read model eventually consistent without a dual write: the event
/// append and the projection intent commit together, so a projection can never be silently skipped. The
/// background <c>PaymentReadModelProjector</c> claims these markers, projects the stream's latest state to
/// SQL, and stamps <see cref="ProcessedOnUtc"/>. The marker carries only the stream id; the projector
/// always replays the full stream, so processing a marker is idempotent and order-independent.
/// </summary>
public sealed class PaymentProjectionOutboxDocument
{
    public const string CollectionName = "payment_projection_outbox";

    [BsonId]
    public ObjectId Id { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid PaymentId { get; set; }

    // Stored as a BSON UTC DateTime (not the default [ticks, offset] array or a string) because the
    // claim poll orders by OccurredOnUtc and range-filters ClaimedOnUtc against the lease cut-off; both
    // need a range-queryable representation. Every timestamp here is UTC, so the conversion is lossless.
    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset OccurredOnUtc { get; set; }

    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset? ProcessedOnUtc { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid? ClaimId { get; set; }

    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset? ClaimedOnUtc { get; set; }
}
