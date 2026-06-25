using FreshCart.BuildingBlocks.Messaging.Outbox;
using MongoDB.Driver;

namespace FreshCart.Delivery.Infrastructure.Persistence;

/// <summary>
/// MongoDB-backed <see cref="IOutboxStore"/>. Outbox messages live in the same database (and are written
/// in the same multi-document transaction) as the delivery documents, which is the whole point of the
/// pattern: the integration event is durable the instant the state change is, so it cannot be lost on a
/// broker failure.
/// </summary>
public sealed class MongoOutboxStore(DeliveryMongoContext context, TimeProvider timeProvider) : IOutboxStore
{
    public async Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int batchSize, CancellationToken cancellationToken)
    {
        var claimId = Guid.NewGuid();
        var nowUtc = timeProvider.GetUtcNow();
        var leaseExpiresBefore = nowUtc - OutboxMessage.ClaimLeaseTimeout;

        var claimable = Builders<OutboxMessage>.Filter.And(
            Builders<OutboxMessage>.Filter.Eq(message => message.ProcessedOnUtc, null),
            Builders<OutboxMessage>.Filter.Or(
                Builders<OutboxMessage>.Filter.Eq(message => message.ClaimId, null),
                Builders<OutboxMessage>.Filter.Lt(message => message.ClaimedOnUtc, leaseExpiresBefore)));

        var candidateIds = await context.Outbox
            .Find(claimable)
            .Sort(Builders<OutboxMessage>.Sort.Ascending(message => message.OccurredOnUtc))
            .Limit(batchSize)
            .Project(message => message.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (candidateIds.Count == 0)
        {
            return [];
        }

        // The claim is what serialises competing publisher replicas: the conditional update only stamps
        // rows still unclaimed (or whose lease lapsed), and MongoDB applies each document update atomically,
        // so of two drainers racing for the same candidates each row is won by exactly one. We then return
        // only the rows this call actually won.
        var claimUpdate = Builders<OutboxMessage>.Update
            .Set(message => message.ClaimId, claimId)
            .Set(message => message.ClaimedOnUtc, nowUtc);

        await context.Outbox
            .UpdateManyAsync(
                Builders<OutboxMessage>.Filter.And(Builders<OutboxMessage>.Filter.In(message => message.Id, candidateIds), claimable),
                claimUpdate,
                options: null,
                cancellationToken)
            .ConfigureAwait(false);

        return await context.Outbox
            .Find(Builders<OutboxMessage>.Filter.And(
                Builders<OutboxMessage>.Filter.Eq(message => message.ClaimId, claimId),
                Builders<OutboxMessage>.Filter.Eq(message => message.ProcessedOnUtc, null)))
            .Sort(Builders<OutboxMessage>.Sort.Ascending(message => message.OccurredOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MarkAsPublishedAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var processedOnUtc = timeProvider.GetUtcNow();
        var writes = new List<WriteModel<OutboxMessage>>();
        foreach (var publishedMessage in messages)
        {
            publishedMessage.ProcessedOnUtc = processedOnUtc;
            publishedMessage.Error = null;

            writes.Add(new UpdateOneModel<OutboxMessage>(
                Builders<OutboxMessage>.Filter.Eq(message => message.Id, publishedMessage.Id),
                Builders<OutboxMessage>.Update
                    .Set(message => message.ProcessedOnUtc, processedOnUtc)
                    .Set(message => message.Error, (string?)null)));
        }

        if (writes.Count == 0)
        {
            return;
        }

        await context.Outbox.BulkWriteAsync(writes, options: null, cancellationToken).ConfigureAwait(false);
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

        return context.Outbox.ReplaceOneAsync(
            Builders<OutboxMessage>.Filter.Eq(stored => stored.Id, message.Id),
            message,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
    }
}
