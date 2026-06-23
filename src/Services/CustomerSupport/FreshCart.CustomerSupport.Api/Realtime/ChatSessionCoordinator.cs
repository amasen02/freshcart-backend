using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.CustomerSupport.Api.Assignment;
using FreshCart.CustomerSupport.Api.Domain;
using FreshCart.CustomerSupport.Api.Persistence;
using Microsoft.Extensions.Logging;

namespace FreshCart.CustomerSupport.Api.Realtime;

/// <summary>
/// All the chat orchestration that does not depend on a live connection: creating sessions, the
/// one-open-session rule, the participant guard, message relay, ending a chat and draining the queue
/// when an agent frees up. Kept as a plain class over repositories, the assignment ports and the
/// notifier so every rule here is unit-testable without spinning up a SignalR host.
/// </summary>
public sealed partial class ChatSessionCoordinator
{
    private const string UnknownAgentDisplayName = "Support agent";

    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IAgentAssignmentStrategy _assignmentStrategy;
    private readonly IAgentAvailabilityRegistry _availabilityRegistry;
    private readonly IChatWaitingLine _waitingLine;
    private readonly ISupportChatNotifier _notifier;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ChatSessionCoordinator> _logger;

    public ChatSessionCoordinator(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IAgentAssignmentStrategy assignmentStrategy,
        IAgentAvailabilityRegistry availabilityRegistry,
        IChatWaitingLine waitingLine,
        ISupportChatNotifier notifier,
        TimeProvider timeProvider,
        ILogger<ChatSessionCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(sessionRepository);
        ArgumentNullException.ThrowIfNull(messageRepository);
        ArgumentNullException.ThrowIfNull(assignmentStrategy);
        ArgumentNullException.ThrowIfNull(availabilityRegistry);
        ArgumentNullException.ThrowIfNull(waitingLine);
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _assignmentStrategy = assignmentStrategy;
        _availabilityRegistry = availabilityRegistry;
        _waitingLine = waitingLine;
        _notifier = notifier;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ChatSessionDto> RequestChatAsync(
        Guid customerId,
        string customerDisplayName,
        string topic,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerDisplayName);

        var normalisedTopic = NormaliseTopic(topic);

        var existingOpenSession = await _sessionRepository
            .GetOpenSessionForCustomerAsync(customerId, cancellationToken)
            .ConfigureAwait(false);

        if (existingOpenSession is not null)
        {
            throw new BadRequestException("You already have an open support chat. End it before starting another.");
        }

        var session = ChatSession.Start(
            Guid.CreateVersion7(),
            normalisedTopic,
            customerId,
            customerDisplayName,
            _timeProvider.GetUtcNow());

        var assignedAgentId = await _assignmentStrategy.AssignAsync(cancellationToken).ConfigureAwait(false);
        if (assignedAgentId is { } agentId)
        {
            session.AssignTo(agentId, await ResolveAgentDisplayNameAsync(agentId, cancellationToken).ConfigureAwait(false));

            try
            {
                await _sessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // AssignAsync already incremented the agent's load, but no session was persisted to
                // reference them, so nothing would ever release it. Compensate the increment before the
                // failure surfaces or the agent drifts to an artificially high load and stops being
                // selected.
                await _assignmentStrategy.ReleaseAsync(agentId, cancellationToken).ConfigureAwait(false);
                throw;
            }

            var assignedDto = ChatSessionDto.FromDomain(session);
            await _notifier.ChatAssignedAsync(customerId, assignedDto, cancellationToken).ConfigureAwait(false);
            await _notifier.ChatAssignedAsync(agentId, assignedDto, cancellationToken).ConfigureAwait(false);

            LogSessionAssigned(session.Id, agentId);
            return assignedDto;
        }

        await _sessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        await _waitingLine.EnqueueAsync(session.Id, cancellationToken).ConfigureAwait(false);

        var queuedDto = ChatSessionDto.FromDomain(session);
        var position = await ResolveQueuePositionAsync(session.Id, cancellationToken).ConfigureAwait(false);
        await _notifier.QueuePositionChangedAsync(customerId, session.Id, position, cancellationToken).ConfigureAwait(false);

        LogSessionQueued(session.Id, position);
        return queuedDto;
    }

    public async Task SendMessageAsync(
        Guid senderId,
        string senderDisplayName,
        SenderRole senderRole,
        Guid sessionId,
        string text,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senderDisplayName);

        var messageText = NormaliseMessage(text);
        var session = await LoadParticipantSessionAsync(sessionId, senderId, cancellationToken).ConfigureAwait(false);

        if (session.Status == SessionStatus.Ended)
        {
            throw new BadRequestException("The chat session has already ended.");
        }

        var message = ChatMessage.Create(
            Guid.CreateVersion7(),
            session.Id,
            senderId,
            senderDisplayName,
            senderRole,
            messageText,
            _timeProvider.GetUtcNow());

        await _messageRepository.AddAsync(message, cancellationToken).ConfigureAwait(false);

        var messageDto = ChatMessageDto.FromDomain(message);
        await _notifier.MessageReceivedAsync(session.CustomerId, messageDto, cancellationToken).ConfigureAwait(false);

        if (session.AgentId is { } agentId)
        {
            await _notifier.MessageReceivedAsync(agentId, messageDto, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RelayTypingAsync(
        Guid senderId,
        string senderDisplayName,
        Guid sessionId,
        bool isTyping,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senderDisplayName);

        var session = await LoadParticipantSessionAsync(sessionId, senderId, cancellationToken).ConfigureAwait(false);

        var otherParticipantId = ResolveOtherParticipant(session, senderId);
        if (otherParticipantId is { } recipientId)
        {
            await _notifier
                .ParticipantTypingAsync(recipientId, session.Id, senderDisplayName, isTyping, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task EndChatAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await LoadParticipantSessionAsync(sessionId, userId, cancellationToken).ConfigureAwait(false);

        if (session.Status == SessionStatus.Queued)
        {
            await _waitingLine.RemoveAsync(session.Id, cancellationToken).ConfigureAwait(false);
        }

        var releasedAgentId = session.AgentId;
        session.End(_timeProvider.GetUtcNow());
        await _sessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);

        await _notifier.ChatEndedAsync(session.CustomerId, session.Id, cancellationToken).ConfigureAwait(false);
        if (releasedAgentId is { } agentId)
        {
            await _notifier.ChatEndedAsync(agentId, session.Id, cancellationToken).ConfigureAwait(false);
            await _assignmentStrategy.ReleaseAsync(agentId, cancellationToken).ConfigureAwait(false);
            await DrainQueueAsync(cancellationToken).ConfigureAwait(false);
        }

        LogSessionEnded(session.Id);
    }

    /// <summary>
    /// Re-queues every active session an agent was working when their last connection dropped, then
    /// removes them from the available pool. Their customers are told they are back in the queue; the
    /// sessions stay open so a customer keeps their place rather than being cut off.
    /// </summary>
    public async Task HandleAgentDisconnectedAsync(Guid agentId, CancellationToken cancellationToken)
    {
        var activeSessions = await _sessionRepository
            .GetActiveSessionsForAgentAsync(agentId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var session in activeSessions)
        {
            session.ReturnToQueue();
            await _sessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
            await _waitingLine.EnqueueAsync(session.Id, cancellationToken).ConfigureAwait(false);
        }

        foreach (var session in activeSessions)
        {
            var position = await ResolveQueuePositionAsync(session.Id, cancellationToken).ConfigureAwait(false);
            await _notifier
                .QueuePositionChangedAsync(session.CustomerId, session.Id, position, cancellationToken)
                .ConfigureAwait(false);
        }

        if (activeSessions.Count > 0)
        {
            LogAgentSessionsReturnedToQueue(agentId, activeSessions.Count);
        }
    }

    /// <summary>
    /// Pulls the next waiting session and hands it to a freed agent, then refreshes the position of
    /// everyone still waiting. Called when an agent connects or releases a chat.
    /// </summary>
    public async Task DrainQueueAsync(CancellationToken cancellationToken)
    {
        var nextSessionId = await _waitingLine.DequeueAsync(cancellationToken).ConfigureAwait(false);
        if (nextSessionId is not { } sessionId)
        {
            return;
        }

        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null || session.Status != SessionStatus.Queued)
        {
            await NotifyRemainingQueuePositionsAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var assignedAgentId = await _assignmentStrategy.AssignAsync(cancellationToken).ConfigureAwait(false);
        if (assignedAgentId is not { } agentId)
        {
            // No agent was free. The session was already popped from the head, so it goes back to the
            // FRONT, never the tail, or a customer who was first in line loses their place whenever a
            // concurrent drain races them for a single freed agent.
            await _waitingLine.RequeueAtFrontAsync(sessionId, cancellationToken).ConfigureAwait(false);
            return;
        }

        session.AssignTo(agentId, await ResolveAgentDisplayNameAsync(agentId, cancellationToken).ConfigureAwait(false));

        try
        {
            await _sessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // The session is out of the queue and the agent's load is already incremented, but the
            // assignment never persisted. Undo both so the customer keeps their place and the agent is
            // not permanently over-counted, then let the failure surface.
            await _assignmentStrategy.ReleaseAsync(agentId, cancellationToken).ConfigureAwait(false);
            await _waitingLine.RequeueAtFrontAsync(sessionId, cancellationToken).ConfigureAwait(false);
            throw;
        }

        var assignedDto = ChatSessionDto.FromDomain(session);
        await _notifier.ChatAssignedAsync(session.CustomerId, assignedDto, cancellationToken).ConfigureAwait(false);
        await _notifier.ChatAssignedAsync(agentId, assignedDto, cancellationToken).ConfigureAwait(false);

        LogSessionAssigned(session.Id, agentId);
        await NotifyRemainingQueuePositionsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task NotifyRemainingQueuePositionsAsync(CancellationToken cancellationToken)
    {
        var waitingSessionIds = await _waitingLine.SnapshotAsync(cancellationToken).ConfigureAwait(false);
        for (var index = 0; index < waitingSessionIds.Count; index++)
        {
            var waitingSession = await _sessionRepository
                .GetByIdAsync(waitingSessionIds[index], cancellationToken)
                .ConfigureAwait(false);

            if (waitingSession is not null)
            {
                await _notifier
                    .QueuePositionChangedAsync(waitingSession.CustomerId, waitingSession.Id, index + 1, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task<ChatSession> LoadParticipantSessionAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ChatSession), sessionId);

        if (!session.IsParticipant(userId))
        {
            throw new ForbiddenException("Sender is not part of this chat session.");
        }

        return session;
    }

    private async Task<int> ResolveQueuePositionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var waitingSessionIds = await _waitingLine.SnapshotAsync(cancellationToken).ConfigureAwait(false);
        return ResolveQueuePosition(waitingSessionIds, sessionId);
    }

    private static int ResolveQueuePosition(IReadOnlyList<Guid> waitingSessionIds, Guid sessionId)
    {
        for (var index = 0; index < waitingSessionIds.Count; index++)
        {
            if (waitingSessionIds[index] == sessionId)
            {
                return index + 1;
            }
        }

        return waitingSessionIds.Count;
    }

    private static Guid? ResolveOtherParticipant(ChatSession session, Guid senderId) =>
        senderId == session.CustomerId ? session.AgentId : session.CustomerId;

    private async Task<string> ResolveAgentDisplayNameAsync(Guid agentId, CancellationToken cancellationToken)
    {
        var displayName = await _availabilityRegistry
            .GetDisplayNameAsync(agentId, cancellationToken)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(displayName) ? UnknownAgentDisplayName : displayName;
    }

    private static string NormaliseTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new BadRequestException("A chat topic is required.");
        }

        var trimmedTopic = topic.Trim();
        return trimmedTopic.Length > SupportDefaults.MaxTopicLength
            ? throw new BadRequestException($"A chat topic cannot exceed {SupportDefaults.MaxTopicLength} characters.")
            : trimmedTopic;
    }

    private static string NormaliseMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new BadRequestException("A message cannot be empty.");
        }

        var trimmedText = text.Trim();
        return trimmedText.Length > SupportDefaults.MaxMessageLength
            ? throw new BadRequestException($"A message cannot exceed {SupportDefaults.MaxMessageLength} characters.")
            : trimmedText;
    }

    [LoggerMessage(EventId = 2100, Level = LogLevel.Information, Message = "Chat session {SessionId} assigned to agent {AgentId}")]
    private partial void LogSessionAssigned(Guid sessionId, Guid agentId);

    [LoggerMessage(EventId = 2101, Level = LogLevel.Information, Message = "Chat session {SessionId} queued at position {Position}")]
    private partial void LogSessionQueued(Guid sessionId, int position);

    [LoggerMessage(EventId = 2102, Level = LogLevel.Information, Message = "Chat session {SessionId} ended")]
    private partial void LogSessionEnded(Guid sessionId);

    [LoggerMessage(EventId = 2103, Level = LogLevel.Warning, Message = "Agent {AgentId} disconnected; {SessionCount} active session(s) returned to the queue")]
    private partial void LogAgentSessionsReturnedToQueue(Guid agentId, int sessionCount);
}
