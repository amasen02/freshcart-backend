using FreshCart.BuildingBlocks.Pagination;
using FreshCart.CustomerSupport.Api.Domain;

namespace FreshCart.CustomerSupport.Api.Persistence;

/// <summary>
/// Persistence port for chat sessions. Upsert rather than separate insert/update because the hub
/// saves a session both at creation and after every state transition; one method keeps callers from
/// having to know whether the document already exists.
/// </summary>
public interface IChatSessionRepository
{
    Task SaveAsync(ChatSession session, CancellationToken cancellationToken);

    Task<ChatSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>The customer's single open (queued or active) session, or null if they have none.</summary>
    Task<ChatSession?> GetOpenSessionForCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    /// <summary>Active sessions currently owned by an agent (used when re-queuing on disconnect).</summary>
    Task<IReadOnlyList<ChatSession>> GetActiveSessionsForAgentAsync(Guid agentId, CancellationToken cancellationToken);

    Task<PaginatedResult<ChatSession>> GetSessionsAsync(
        SessionStatus? statusFilter,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken);
}
