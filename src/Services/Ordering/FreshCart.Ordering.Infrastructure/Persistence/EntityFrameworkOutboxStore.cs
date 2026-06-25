using FreshCart.BuildingBlocks.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Ordering.Infrastructure.Persistence;

/// <summary>
/// EF Core-backed <see cref="IOutboxStore"/>. The outbox table lives in the same database as the
/// Order aggregate, so the publisher reads from the same source of truth the writers commit to.
/// </summary>
public sealed class EntityFrameworkOutboxStore(OrderingDbContext dbContext, TimeProvider timeProvider) : IOutboxStore
{
    public async Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int batchSize, CancellationToken cancellationToken)
    {
        var claimId = Guid.NewGuid();
        var nowUtc = timeProvider.GetUtcNow();
        var leaseExpiresBefore = nowUtc - OutboxMessage.ClaimLeaseTimeout;

        var candidateIds = await dbContext.OutboxMessages
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

        // The claim is what serialises competing publisher replicas: the conditional UPDATE only stamps
        // rows that are still unclaimed (or whose lease lapsed), so of two drainers racing for the same
        // candidates each row is won by exactly one. We then return only the rows this call actually won.
        await dbContext.OutboxMessages
            .Where(message => candidateIds.Contains(message.Id)
                && message.ProcessedOnUtc == null
                && (message.ClaimId == null || message.ClaimedOnUtc < leaseExpiresBefore))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.ClaimId, claimId)
                    .SetProperty(message => message.ClaimedOnUtc, nowUtc),
                cancellationToken)
            .ConfigureAwait(false);

        return await dbContext.OutboxMessages
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
        }

        return dbContext.SaveChangesAsync(cancellationToken);
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

        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
