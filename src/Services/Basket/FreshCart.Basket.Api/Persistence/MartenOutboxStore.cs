using FreshCart.BuildingBlocks.Messaging.Outbox;
using Marten;
using Marten.Patching;

namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Marten-backed <see cref="IOutboxStore"/>. Outbox messages live as documents in the same
/// PostgreSQL database (and the same checkout transaction) as the basket documents, which is the
/// whole point of the pattern.
/// </summary>
public sealed class MartenOutboxStore(IDocumentSession documentSession, TimeProvider timeProvider) : IOutboxStore
{
    public async Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int batchSize, CancellationToken cancellationToken)
    {
        var claimId = Guid.NewGuid();
        var nowUtc = timeProvider.GetUtcNow();
        var leaseExpiresBefore = nowUtc - OutboxMessage.ClaimLeaseTimeout;

        var candidateIds = await documentSession.Query<OutboxMessage>()
            .Where(message => message.ProcessedOnUtc == null
                && (message.ClaimId == null || message.ClaimedOnUtc < leaseExpiresBefore))
            .OrderBy(message => message.OccurredOnUtc)
            .Take(batchSize)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (candidateIds.Count == 0)
        {
            return [];
        }

        // The claim serialises competing publisher replicas: the conditional patch stamps only rows that
        // are still unclaimed (or whose lease lapsed), so each candidate row is won by exactly one drainer.
        // We then return only the rows this call actually won.
        documentSession.Patch<OutboxMessage>(message => candidateIds.Contains(message.Id)
                && message.ProcessedOnUtc == null
                && (message.ClaimId == null || message.ClaimedOnUtc < leaseExpiresBefore))
            .Set(message => message.ClaimId, claimId)
            .Set(message => message.ClaimedOnUtc, nowUtc);
        await documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await documentSession.Query<OutboxMessage>()
            .Where(message => message.ClaimId == claimId && message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

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

        var deadLettered = message.MarkFailed(error, maxRetryAttempts, timeProvider.GetUtcNow());
        if (!deadLettered)
        {
            message.ReleaseClaim();
        }

        documentSession.Store(message);

        return documentSession.SaveChangesAsync(cancellationToken);
    }
}
