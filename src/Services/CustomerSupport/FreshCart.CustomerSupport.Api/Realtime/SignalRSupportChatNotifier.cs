using Microsoft.AspNetCore.SignalR;

namespace FreshCart.CustomerSupport.Api.Realtime;

/// <summary>
/// Sends the server-to-client events through the hub context, addressing each recipient by their
/// <c>user:{userId}</c> group so the message reaches every connection that person has open. The
/// coordinator depends on the interface, not this type, which is what keeps the orchestration tests
/// free of a SignalR runtime.
/// </summary>
public sealed class SignalRSupportChatNotifier : ISupportChatNotifier
{
    private readonly IHubContext<SupportChatHub> _hubContext;

    public SignalRSupportChatNotifier(IHubContext<SupportChatHub> hubContext)
    {
        ArgumentNullException.ThrowIfNull(hubContext);

        _hubContext = hubContext;
    }

    public Task ChatAssignedAsync(Guid recipientUserId, ChatSessionDto session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        return SendToUserAsync(recipientUserId, SupportHubMethodNames.ChatAssigned, [session], cancellationToken);
    }

    public Task MessageReceivedAsync(Guid recipientUserId, ChatMessageDto message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        return SendToUserAsync(recipientUserId, SupportHubMethodNames.MessageReceived, [message], cancellationToken);
    }

    public Task ParticipantTypingAsync(
        Guid recipientUserId,
        Guid sessionId,
        string displayName,
        bool isTyping,
        CancellationToken cancellationToken) =>
        SendToUserAsync(
            recipientUserId,
            SupportHubMethodNames.ParticipantTyping,
            [sessionId.ToString(), displayName, isTyping],
            cancellationToken);

    public Task ChatEndedAsync(Guid recipientUserId, Guid sessionId, CancellationToken cancellationToken) =>
        SendToUserAsync(
            recipientUserId,
            SupportHubMethodNames.ChatEnded,
            [sessionId.ToString()],
            cancellationToken);

    public Task QueuePositionChangedAsync(
        Guid recipientUserId,
        Guid sessionId,
        int position,
        CancellationToken cancellationToken) =>
        SendToUserAsync(
            recipientUserId,
            SupportHubMethodNames.QueuePositionChanged,
            [sessionId.ToString(), position],
            cancellationToken);

    private Task SendToUserAsync(
        Guid recipientUserId,
        string methodName,
        object?[] arguments,
        CancellationToken cancellationToken)
    {
        // The token belongs to the SENDER's connection. Delivering an already-persisted event to the
        // OTHER participant must not be cancelled because the sender closed their tab, so the recipient
        // fan-out runs under CancellationToken.None. The sender's token is still honoured upstream for
        // the persistence work that precedes this relay.
        _ = cancellationToken;

        return _hubContext.Clients
            .Group(SupportGroupNames.ForUser(recipientUserId))
            .SendCoreAsync(methodName, arguments, CancellationToken.None);
    }
}
