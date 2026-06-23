using FreshCart.BuildingBlocks.Pagination;
using FreshCart.CustomerSupport.Api.Domain;
using MongoDB.Driver;

namespace FreshCart.CustomerSupport.Api.Persistence;

/// <summary>
/// MongoDB-backed session store. The open-session lookup and the agent-active lookup both lean on the
/// (CustomerId, StartedOnUtc) and (AgentId, Status) indexes the initializer creates.
/// </summary>
public sealed class MongoChatSessionRepository : IChatSessionRepository
{
    private static readonly SessionStatus[] OpenStatuses = [SessionStatus.Queued, SessionStatus.Active];

    private readonly IMongoCollection<ChatSessionDocument> _sessions;

    public MongoChatSessionRepository(SupportChatMongoContext mongoContext)
    {
        ArgumentNullException.ThrowIfNull(mongoContext);

        _sessions = mongoContext.Sessions;
    }

    public Task SaveAsync(ChatSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var document = ChatSessionDocument.FromDomain(session);
        return _sessions.ReplaceOneAsync(
            stored => stored.Id == session.Id,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<ChatSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var document = await _sessions
            .Find(stored => stored.Id == sessionId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document?.ToDomain();
    }

    public async Task<ChatSession?> GetOpenSessionForCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var filterBuilder = Builders<ChatSessionDocument>.Filter;
        var openSessionForCustomer = filterBuilder.And(
            filterBuilder.Eq(stored => stored.CustomerId, customerId),
            filterBuilder.In(stored => stored.Status, OpenStatuses));

        var document = await _sessions
            .Find(openSessionForCustomer)
            .SortByDescending(stored => stored.StartedOnUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<ChatSession>> GetActiveSessionsForAgentAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var documents = await _sessions
            .Find(stored => stored.AgentId == agentId && stored.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return documents.ConvertAll(document => document.ToDomain());
    }

    public async Task<PaginatedResult<ChatSession>> GetSessionsAsync(
        SessionStatus? statusFilter,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var normalisedRequest = paginationRequest.Normalise();

        var filter = statusFilter is null
            ? Builders<ChatSessionDocument>.Filter.Empty
            : Builders<ChatSessionDocument>.Filter.Eq(stored => stored.Status, statusFilter.Value);

        var totalItemCount = await _sessions
            .CountDocumentsAsync(filter, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var documents = await _sessions
            .Find(filter)
            .SortByDescending(stored => stored.StartedOnUtc)
            .Skip((normalisedRequest.PageNumber - 1) * normalisedRequest.PageSize)
            .Limit(normalisedRequest.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PaginatedResult<ChatSession>(
            normalisedRequest.PageNumber,
            normalisedRequest.PageSize,
            totalItemCount,
            documents.ConvertAll(document => document.ToDomain()));
    }
}
