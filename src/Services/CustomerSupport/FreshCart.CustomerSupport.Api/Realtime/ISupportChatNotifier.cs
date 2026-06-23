namespace FreshCart.CustomerSupport.Api.Realtime;

/// <summary>
/// Outbound side of the support hub, named after the five server-to-client events in the contract.
/// The coordinator depends on this rather than on <c>IHubContext</c> directly so its orchestration
/// (assignment, queue draining, the participant guard) is unit-testable without a SignalR runtime.
/// </summary>
public interface ISupportChatNotifier
{
    Task ChatAssignedAsync(Guid recipientUserId, ChatSessionDto session, CancellationToken cancellationToken);

    Task MessageReceivedAsync(Guid recipientUserId, ChatMessageDto message, CancellationToken cancellationToken);

    Task ParticipantTypingAsync(
        Guid recipientUserId,
        Guid sessionId,
        string displayName,
        bool isTyping,
        CancellationToken cancellationToken);

    Task ChatEndedAsync(Guid recipientUserId, Guid sessionId, CancellationToken cancellationToken);

    Task QueuePositionChangedAsync(
        Guid recipientUserId,
        Guid sessionId,
        int position,
        CancellationToken cancellationToken);
}
