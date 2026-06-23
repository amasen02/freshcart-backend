using FreshCart.CustomerSupport.Api.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FreshCart.CustomerSupport.Api.Persistence;

/// <summary>
/// Storage shape of a <see cref="ChatSession"/>. Guids use the globally registered Standard UUID
/// representation so the (CustomerId, StartedOnUtc) and (AgentId, Status) indexes match on the same
/// binary subtype the driver writes; timestamps are stored as ISO strings so a transcript export
/// reads naturally.
/// </summary>
public sealed class ChatSessionDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public string Topic { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerDisplayName { get; set; } = string.Empty;

    public Guid? AgentId { get; set; }

    public string? AgentDisplayName { get; set; }

    [BsonRepresentation(BsonType.String)]
    public SessionStatus Status { get; set; }

    [BsonRepresentation(BsonType.String)]
    public DateTimeOffset StartedOnUtc { get; set; }

    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public DateTimeOffset? EndedOnUtc { get; set; }

    public static ChatSessionDocument FromDomain(ChatSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new ChatSessionDocument
        {
            Id = session.Id,
            Topic = session.Topic,
            CustomerId = session.CustomerId,
            CustomerDisplayName = session.CustomerDisplayName,
            AgentId = session.AgentId,
            AgentDisplayName = session.AgentDisplayName,
            Status = session.Status,
            StartedOnUtc = session.StartedOnUtc,
            EndedOnUtc = session.EndedOnUtc,
        };
    }

    public ChatSession ToDomain() => ChatSession.Rehydrate(
        Id,
        Topic,
        CustomerId,
        CustomerDisplayName,
        AgentId,
        AgentDisplayName,
        Status,
        StartedOnUtc,
        EndedOnUtc);
}
