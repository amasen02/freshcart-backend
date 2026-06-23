using FreshCart.BuildingBlocks.Messaging.Outbox;
using Marten;

namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Marten-backed <see cref="IOutboxStore"/>. Outbox messages live as documents in the same
/// PostgreSQL database (and the same checkout transaction) as the basket documents, which is the
/// whole point of the pattern.
/// </summary>
public sealed class MartenOutboxStore(IDocumentSession documentSession, TimeProvider timeProvider) : IOutboxStore
{
    public Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int batchSize, CancellationToken cancellationToken) =>
        documentSession.Query<OutboxMessage>()
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public Task MarkAsPublishedAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var processedOnUtc = timeProvider.GetUtcNow();
        foreach (var publishedMessage in messages)
        {
            publishedMessage.ProcessedOnUtc = processedOnUtc;
            publishedMessage.Error = null;
            documentSession.Store(publishedMessage);
        }

        return documentSession.SaveChangesAsync(cancellationToken);
    }

    public Task MarkAsFailedAsync(OutboxMessage message, string error, int maxRetryAttempts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(error);

        message.MarkFailed(error, maxRetryAttempts, timeProvider.GetUtcNow());
        documentSession.Store(message);

        return documentSession.SaveChangesAsync(cancellationToken);
    }
}
