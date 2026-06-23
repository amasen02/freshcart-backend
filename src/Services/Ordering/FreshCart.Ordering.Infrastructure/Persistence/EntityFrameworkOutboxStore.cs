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
        => await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

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

        message.MarkFailed(error, maxRetryAttempts, timeProvider.GetUtcNow());

        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
