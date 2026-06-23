using FreshCart.BuildingBlocks.Pagination;
using FreshCart.CustomerSupport.Api.Domain;
using MongoDB.Driver;

namespace FreshCart.CustomerSupport.Api.Persistence;

/// <summary>
/// MongoDB-backed transcript store. Paging sorts ascending by send time so the (SessionId, SentOnUtc)
/// index serves the range directly without an in-memory sort.
/// </summary>
public sealed class MongoChatMessageRepository : IChatMessageRepository
{
    private readonly IMongoCollection<ChatMessageDocument> _messages;

    public MongoChatMessageRepository(SupportChatMongoContext mongoContext)
    {
        ArgumentNullException.ThrowIfNull(mongoContext);

        _messages = mongoContext.Messages;
    }

    public Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var document = ChatMessageDocument.FromDomain(message);
        return _messages.InsertOneAsync(document, options: null, cancellationToken);
    }

    public async Task<PaginatedResult<ChatMessage>> GetSessionMessagesAsync(
        Guid sessionId,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var normalisedRequest = paginationRequest.Normalise();
        var filter = Builders<ChatMessageDocument>.Filter.Eq(stored => stored.SessionId, sessionId);

        var totalItemCount = await _messages
            .CountDocumentsAsync(filter, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var documents = await _messages
            .Find(filter)
            .SortBy(stored => stored.SentOnUtc)
            .Skip((normalisedRequest.PageNumber - 1) * normalisedRequest.PageSize)
            .Limit(normalisedRequest.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PaginatedResult<ChatMessage>(
            normalisedRequest.PageNumber,
            normalisedRequest.PageSize,
            totalItemCount,
            documents.ConvertAll(document => document.ToDomain()));
    }
}
