using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.CustomerSupport.Api.Domain;

/// <summary>
/// A support conversation between one customer and (at most) one agent. The session owns its own
/// state transitions so an illegal move (assigning an ended session, ending one twice) is rejected
/// here rather than relying on every caller to check the status first.
/// </summary>
public sealed class ChatSession
{
    private ChatSession(
        Guid id,
        string topic,
        Guid customerId,
        string customerDisplayName,
        SessionStatus status,
        DateTimeOffset startedOnUtc)
    {
        Id = id;
        Topic = topic;
        CustomerId = customerId;
        CustomerDisplayName = customerDisplayName;
        Status = status;
        StartedOnUtc = startedOnUtc;
    }

    public Guid Id { get; }

    public string Topic { get; }

    public Guid CustomerId { get; }

    public string CustomerDisplayName { get; }

    public Guid? AgentId { get; private set; }

    public string? AgentDisplayName { get; private set; }

    public SessionStatus Status { get; private set; }

    public DateTimeOffset StartedOnUtc { get; }

    public DateTimeOffset? EndedOnUtc { get; private set; }

    public static ChatSession Start(
        Guid id,
        string topic,
        Guid customerId,
        string customerDisplayName,
        DateTimeOffset startedOnUtc) =>
        new(id, topic, customerId, customerDisplayName, SessionStatus.Queued, startedOnUtc);

    public static ChatSession Rehydrate(
        Guid id,
        string topic,
        Guid customerId,
        string customerDisplayName,
        Guid? agentId,
        string? agentDisplayName,
        SessionStatus status,
        DateTimeOffset startedOnUtc,
        DateTimeOffset? endedOnUtc) =>
        new(id, topic, customerId, customerDisplayName, status, startedOnUtc)
        {
            AgentId = agentId,
            AgentDisplayName = agentDisplayName,
            EndedOnUtc = endedOnUtc,
        };

    public void AssignTo(Guid agentId, string agentDisplayName)
    {
        if (Status == SessionStatus.Ended)
        {
            throw new BadRequestException("An ended chat session cannot be assigned to an agent.");
        }

        AgentId = agentId;
        AgentDisplayName = agentDisplayName;
        Status = SessionStatus.Active;
    }

    public void ReturnToQueue()
    {
        if (Status == SessionStatus.Ended)
        {
            throw new BadRequestException("An ended chat session cannot be returned to the queue.");
        }

        AgentId = null;
        AgentDisplayName = null;
        Status = SessionStatus.Queued;
    }

    public void End(DateTimeOffset endedOnUtc)
    {
        if (Status == SessionStatus.Ended)
        {
            throw new BadRequestException("The chat session has already ended.");
        }

        Status = SessionStatus.Ended;
        EndedOnUtc = endedOnUtc;
    }

    public bool IsParticipant(Guid userId) => userId == CustomerId || userId == AgentId;
}
