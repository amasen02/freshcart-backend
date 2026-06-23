using FreshCart.CustomerSupport.Api.Domain;

namespace FreshCart.CustomerSupport.Api.Realtime;

/// <summary>
/// Wire shape of a single transcript message for the SignalR clients and the REST history endpoint.
/// </summary>
public sealed record ChatMessageDto(
    string MessageId,
    string SessionId,
    string SenderId,
    string SenderDisplayName,
    string SenderRole,
    string Text,
    DateTimeOffset SentOnUtc)
{
    public static ChatMessageDto FromDomain(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new ChatMessageDto(
            message.Id.ToString(),
            message.SessionId.ToString(),
            message.SenderId.ToString(),
            message.SenderDisplayName,
            message.SenderRole.ToString(),
            message.Text,
            message.SentOnUtc);
    }
}
