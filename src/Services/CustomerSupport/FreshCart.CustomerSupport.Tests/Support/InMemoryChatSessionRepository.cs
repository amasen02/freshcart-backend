using FreshCart.BuildingBlocks.Pagination;
using FreshCart.CustomerSupport.Api.Domain;
using FreshCart.CustomerSupport.Api.Persistence;

namespace FreshCart.CustomerSupport.Tests.Support;

/// <summary>
/// In-memory session store for coordinator orchestration tests. Stores the last-saved snapshot per
/// id so a re-read after a state transition observes the new status, exactly as Mongo would.
/// </summary>
public sealed class InMemoryChatSessionRepository : IChatSessionRepository
{
    private readonly Dictionary<Guid, ChatSession> _sessions = [];
    private bool _failNextSave;

    /// <summary>Arms a single SaveAsync to throw, so tests can drive the assign-then-persist failure path.</summary>
    public void FailNextSave() => _failNextSave = true;

    public Task SaveAsync(ChatSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (_failNextSave)
        {
            _failNextSave = false;
            return Task.FromException(new InvalidOperationException("Simulated persistence failure."));
        }

        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task<ChatSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken) =>
        Task.FromResult(_sessions.GetValueOrDefault(sessionId));

    public Task<ChatSession?> GetOpenSessionForCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var openSession = _sessions.Values
            .Where(session => session.CustomerId == customerId
                && session.Status is SessionStatus.Queued or SessionStatus.Active)
            .OrderByDescending(session => session.StartedOnUtc)
            .FirstOrDefault();

        return Task.FromResult(openSession);
    }

    public Task<IReadOnlyList<ChatSession>> GetActiveSessionsForAgentAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatSession> activeSessions = _sessions.Values
            .Where(session => session.AgentId == agentId && session.Status == SessionStatus.Active)
            .ToList();

        return Task.FromResult(activeSessions);
    }

    public Task<PaginatedResult<ChatSession>> GetSessionsAsync(
        SessionStatus? statusFilter,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var normalisedRequest = paginationRequest.Normalise();
        var matching = _sessions.Values
            .Where(session => statusFilter is null || session.Status == statusFilter.Value)
            .OrderByDescending(session => session.StartedOnUtc)
            .ToList();

        var page = matching
            .Skip((normalisedRequest.PageNumber - 1) * normalisedRequest.PageSize)
            .Take(normalisedRequest.PageSize)
            .ToList();

        return Task.FromResult(new PaginatedResult<ChatSession>(
            normalisedRequest.PageNumber,
            normalisedRequest.PageSize,
            matching.Count,
            page));
    }
}
