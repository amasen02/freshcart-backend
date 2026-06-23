using FreshCart.CustomerSupport.Api.Domain;

namespace FreshCart.CustomerSupport.Api.Realtime;

/// <summary>
/// Wire shape of a chat session for the SignalR clients and the REST endpoints. Identifiers travel as
/// strings and the status as its name to match the pinned hub contract exactly; the Angular client
/// parses them, the server never relies on the client to round-trip them.
/// </summary>
public sealed record ChatSessionDto(
    string SessionId,
    string Topic,
    string CustomerId,
    string CustomerDisplayName,
    string? AgentId,
    string? AgentDisplayName,
    string Status,
    DateTimeOffset StartedOnUtc)
{
    public static ChatSessionDto FromDomain(ChatSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new ChatSessionDto(
            session.Id.ToString(),
            session.Topic,
            session.CustomerId.ToString(),
            session.CustomerDisplayName,
            session.AgentId?.ToString(),
            session.AgentDisplayName,
            session.Status.ToString(),
            session.StartedOnUtc);
    }
}
