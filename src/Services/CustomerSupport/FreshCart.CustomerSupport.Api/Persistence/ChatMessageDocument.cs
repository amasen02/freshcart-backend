using FreshCart.CustomerSupport.Api.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FreshCart.CustomerSupport.Api.Persistence;

/// <summary>
/// Storage shape of a <see cref="ChatMessage"/>. The (SessionId, SentOnUtc) index makes paging a
/// transcript a covered range scan instead of a per-page sort over the whole collection. Guids use
/// the globally registered Standard UUID representation.
/// </summary>
public sealed class ChatMessageDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public Guid SenderId { get; set; }

    public string SenderDisplayName { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public SenderRole SenderRole { get; set; }

    public string Text { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public DateTimeOffset SentOnUtc { get; set; }

    public static ChatMessageDocument FromDomain(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new ChatMessageDocument
        {
            Id = message.Id,
            SessionId = message.SessionId,
            SenderId = message.SenderId,
            SenderDisplayName = message.SenderDisplayName,
            SenderRole = message.SenderRole,
            Text = message.Text,
            SentOnUtc = message.SentOnUtc,
        };
    }

    public ChatMessage ToDomain() => ChatMessage.Create(
        Id,
        SessionId,
        SenderId,
        SenderDisplayName,
        SenderRole,
        Text,
        SentOnUtc);
}
