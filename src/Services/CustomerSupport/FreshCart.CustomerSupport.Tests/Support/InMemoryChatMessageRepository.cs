using FreshCart.BuildingBlocks.Pagination;
using FreshCart.CustomerSupport.Api.Domain;
using FreshCart.CustomerSupport.Api.Persistence;

namespace FreshCart.CustomerSupport.Tests.Support;

/// <summary>
/// In-memory transcript store for coordinator tests. Exposes the captured messages so a test can
/// assert that a relayed message was actually persisted, not just broadcast.
/// </summary>
public sealed class InMemoryChatMessageRepository : IChatMessageRepository
{
    private readonly List<ChatMessage> _messages = [];

    public IReadOnlyList<ChatMessage> StoredMessages => _messages;

    public Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        _messages.Add(message);
        return Task.CompletedTask;
    }

    public Task<PaginatedResult<ChatMessage>> GetSessionMessagesAsync(
        Guid sessionId,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var normalisedRequest = paginationRequest.Normalise();
        var ordered = _messages
            .Where(message => message.SessionId == sessionId)
            .OrderBy(message => message.SentOnUtc)
            .ToList();

        var page = ordered
            .Skip((normalisedRequest.PageNumber - 1) * normalisedRequest.PageSize)
            .Take(normalisedRequest.PageSize)
            .ToList();

        return Task.FromResult(new PaginatedResult<ChatMessage>(
            normalisedRequest.PageNumber,
            normalisedRequest.PageSize,
            ordered.Count,
            page));
    }
}
