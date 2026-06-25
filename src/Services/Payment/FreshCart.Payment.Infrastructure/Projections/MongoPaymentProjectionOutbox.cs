using MongoDB.Driver;

namespace FreshCart.Payment.Infrastructure.Projections;

/// <summary>
/// Claim-by-update access to the projection-outbox collection, mirroring the multi-instance guard used by
/// the messaging outbox: a conditional <c>UpdateMany</c> stamps a per-cycle claim only on markers that are
/// still unprocessed and unclaimed (or whose lease lapsed), so two projector replicas draining
/// concurrently take disjoint batches and a marker stranded by a crashed replica is re-taken once its
/// lease expires. A projection is never dead-lettered: the read model must converge, so a failed
/// projection releases its claim and is retried.
/// </summary>
public sealed class MongoPaymentProjectionOutbox(IMongoDatabase database, TimeProvider timeProvider)
{
    private static readonly TimeSpan ClaimLeaseTimeout = TimeSpan.FromMinutes(2);
    private const string PollIndexName = "projection_outbox_pending_poll";

    private readonly IMongoCollection<PaymentProjectionOutboxDocument> _collection =
        database.GetCollection<PaymentProjectionOutboxDocument>(PaymentProjectionOutboxDocument.CollectionName);

    public Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var pollIndex = new CreateIndexModel<PaymentProjectionOutboxDocument>(
            Builders<PaymentProjectionOutboxDocument>.IndexKeys
                .Ascending(marker => marker.ProcessedOnUtc)
                .Ascending(marker => marker.OccurredOnUtc),
            new CreateIndexOptions { Name = PollIndexName });

        return _collection.Indexes.CreateOneAsync(pollIndex, options: null, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentProjectionOutboxDocument>> ClaimPendingAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        var claimId = Guid.NewGuid();
        var nowUtc = timeProvider.GetUtcNow();
        var leaseExpiresBefore = nowUtc - ClaimLeaseTimeout;

        var claimable = Builders<PaymentProjectionOutboxDocument>.Filter.And(
            Builders<PaymentProjectionOutboxDocument>.Filter.Eq(marker => marker.ProcessedOnUtc, null),
            Builders<PaymentProjectionOutboxDocument>.Filter.Or(
                Builders<PaymentProjectionOutboxDocument>.Filter.Eq(marker => marker.ClaimId, null),
                Builders<PaymentProjectionOutboxDocument>.Filter.Lt(marker => marker.ClaimedOnUtc, leaseExpiresBefore)));

        var candidateIds = await _collection
            .Find(claimable)
            .Sort(Builders<PaymentProjectionOutboxDocument>.Sort.Ascending(marker => marker.OccurredOnUtc))
            .Limit(batchSize)
            .Project(marker => marker.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (candidateIds.Count == 0)
        {
            return [];
        }

        await _collection
            .UpdateManyAsync(
                Builders<PaymentProjectionOutboxDocument>.Filter.And(
                    Builders<PaymentProjectionOutboxDocument>.Filter.In(marker => marker.Id, candidateIds),
                    claimable),
                Builders<PaymentProjectionOutboxDocument>.Update
                    .Set(marker => marker.ClaimId, claimId)
                    .Set(marker => marker.ClaimedOnUtc, nowUtc),
                options: null,
                cancellationToken)
            .ConfigureAwait(false);

        return await _collection
            .Find(Builders<PaymentProjectionOutboxDocument>.Filter.And(
                Builders<PaymentProjectionOutboxDocument>.Filter.Eq(marker => marker.ClaimId, claimId),
                Builders<PaymentProjectionOutboxDocument>.Filter.Eq(marker => marker.ProcessedOnUtc, null)))
            .Sort(Builders<PaymentProjectionOutboxDocument>.Sort.Ascending(marker => marker.OccurredOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MarkProjectedAsync(
        IEnumerable<PaymentProjectionOutboxDocument> claimedMarkers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claimedMarkers);

        var processedOnUtc = timeProvider.GetUtcNow();
        var writes = claimedMarkers
            .Select(marker => new UpdateOneModel<PaymentProjectionOutboxDocument>(
                Builders<PaymentProjectionOutboxDocument>.Filter.And(
                    Builders<PaymentProjectionOutboxDocument>.Filter.Eq(stored => stored.Id, marker.Id),
                    Builders<PaymentProjectionOutboxDocument>.Filter.Eq(stored => stored.ClaimId, marker.ClaimId)),
                Builders<PaymentProjectionOutboxDocument>.Update.Set(stored => stored.ProcessedOnUtc, processedOnUtc)))
            .Cast<WriteModel<PaymentProjectionOutboxDocument>>()
            .ToList();

        if (writes.Count == 0)
        {
            return;
        }

        await _collection.BulkWriteAsync(writes, options: null, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(
        IEnumerable<PaymentProjectionOutboxDocument> claimedMarkers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claimedMarkers);

        var markerIds = claimedMarkers.Select(marker => marker.Id).ToList();
        if (markerIds.Count == 0)
        {
            return;
        }

        await _collection
            .UpdateManyAsync(
                Builders<PaymentProjectionOutboxDocument>.Filter.In(marker => marker.Id, markerIds),
                Builders<PaymentProjectionOutboxDocument>.Update
                    .Set(marker => marker.ClaimId, (Guid?)null)
                    .Set(marker => marker.ClaimedOnUtc, (DateTimeOffset?)null),
                options: null,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
