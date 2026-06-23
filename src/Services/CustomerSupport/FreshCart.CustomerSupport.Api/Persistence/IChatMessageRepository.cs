using FreshCart.BuildingBlocks.Pagination;
using FreshCart.CustomerSupport.Api.Domain;

namespace FreshCart.CustomerSupport.Api.Persistence;

/// <summary>
/// Persistence port for transcript messages. Reads page ascending by send time so a transcript
/// renders top to bottom in conversation order, served by the (SessionId, SentOnUtc) index.
/// </summary>
public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken);

    Task<PaginatedResult<ChatMessage>> GetSessionMessagesAsync(
        Guid sessionId,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken);
}
