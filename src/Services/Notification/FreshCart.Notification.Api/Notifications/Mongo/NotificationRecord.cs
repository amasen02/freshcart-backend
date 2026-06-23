using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FreshCart.Notification.Api.Notifications.Mongo;

/// <summary>
/// MongoDB storage envelope for a notification. The driver requires an explicit Guid representation
/// before it serialises a Guid; Standard is the cross-driver UUID binary subtype the
/// (UserId, SourceEventId) unique index relies on.
/// </summary>
public sealed class NotificationRecord
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid UserId { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid SourceEventId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid? OrderId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public DateTimeOffset CreatedOnUtc { get; set; }

    public bool IsRead { get; set; }

    public static NotificationRecord FromDocument(NotificationDocument notification) => new()
    {
        Id = notification.Id,
        UserId = notification.UserId,
        SourceEventId = notification.SourceEventId,
        Type = notification.Type,
        Title = notification.Title,
        Message = notification.Message,
        OrderId = notification.OrderId,
        CreatedOnUtc = notification.CreatedOnUtc,
        IsRead = notification.IsRead,
    };

    public NotificationDocument ToDocument() => new()
    {
        Id = Id,
        UserId = UserId,
        SourceEventId = SourceEventId,
        Type = Type,
        Title = Title,
        Message = Message,
        OrderId = OrderId,
        CreatedOnUtc = CreatedOnUtc,
        IsRead = IsRead,
    };
}
