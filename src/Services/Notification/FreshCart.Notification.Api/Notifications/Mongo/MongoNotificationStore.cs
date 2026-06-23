using FreshCart.BuildingBlocks.Pagination;
using MongoDB.Driver;

namespace FreshCart.Notification.Api.Notifications.Mongo;

/// <summary>
/// MongoDB-backed notification history. Idempotency is enforced by the unique compound index over
/// (UserId, SourceEventId): a redelivered event turns the insert into a duplicate-key error, which is
/// translated into <see cref="AddNotificationOutcome.DuplicateIgnored"/> rather than propagated, so a
/// consumer does not re-notify. Every read and the read-receipt are scoped to the owning user.
/// </summary>
public sealed class MongoNotificationStore : INotificationStore
{
    public const string CollectionName = "notifications";

    private const string IdempotencyIndexName = "UX_notifications_UserId_SourceEventId";
    private const string UserTimelineIndexName = "IX_notifications_UserId_CreatedOnUtc_desc";
    private const string OrderRecipientIndexName = "IX_notifications_OrderId";

    private readonly IMongoCollection<NotificationRecord> notifications;

    public MongoNotificationStore(IMongoDatabase notificationDatabase)
    {
        ArgumentNullException.ThrowIfNull(notificationDatabase);

        notifications = notificationDatabase.GetCollection<NotificationRecord>(CollectionName);
    }

    public Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var idempotencyIndex = new CreateIndexModel<NotificationRecord>(
            Builders<NotificationRecord>.IndexKeys
                .Ascending(record => record.UserId)
                .Ascending(record => record.SourceEventId),
            new CreateIndexOptions { Unique = true, Name = IdempotencyIndexName });

        var userTimelineIndex = new CreateIndexModel<NotificationRecord>(
            Builders<NotificationRecord>.IndexKeys
                .Ascending(record => record.UserId)
                .Descending(record => record.CreatedOnUtc),
            new CreateIndexOptions { Name = UserTimelineIndexName });

        var orderRecipientIndex = new CreateIndexModel<NotificationRecord>(
            Builders<NotificationRecord>.IndexKeys.Ascending(record => record.OrderId),
            new CreateIndexOptions { Name = OrderRecipientIndexName });

        return notifications.Indexes.CreateManyAsync(
            [idempotencyIndex, userTimelineIndex, orderRecipientIndex],
            cancellationToken);
    }

    public async Task<AddNotificationOutcome> AddAsync(
        NotificationDocument notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        try
        {
            await notifications
                .InsertOneAsync(NotificationRecord.FromDocument(notification), options: null, cancellationToken)
                .ConfigureAwait(false);

            return AddNotificationOutcome.Stored;
        }
        catch (MongoWriteException writeException) when (
            writeException.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return AddNotificationOutcome.DuplicateIgnored;
        }
    }

    public async Task<PaginatedResult<NotificationDocument>> GetForUserAsync(
        Guid userId,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var normalisedRequest = paginationRequest.Normalise();
        var ownedByUser = Builders<NotificationRecord>.Filter.Eq(record => record.UserId, userId);

        var totalItemCount = await notifications
            .CountDocumentsAsync(ownedByUser, options: null, cancellationToken)
            .ConfigureAwait(false);

        var pageRecords = await notifications
            .Find(ownedByUser)
            .SortByDescending(record => record.CreatedOnUtc)
            .Skip((normalisedRequest.PageNumber - 1) * normalisedRequest.PageSize)
            .Limit(normalisedRequest.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = pageRecords.Select(record => record.ToDocument()).ToArray();

        return new PaginatedResult<NotificationDocument>(
            normalisedRequest.PageNumber,
            normalisedRequest.PageSize,
            totalItemCount,
            items);
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var ownedNotification = Builders<NotificationRecord>.Filter.And(
            Builders<NotificationRecord>.Filter.Eq(record => record.Id, notificationId),
            Builders<NotificationRecord>.Filter.Eq(record => record.UserId, userId));

        var markRead = Builders<NotificationRecord>.Update.Set(record => record.IsRead, true);

        var updateResult = await notifications
            .UpdateOneAsync(ownedNotification, markRead, options: null, cancellationToken)
            .ConfigureAwait(false);

        return updateResult.ModifiedCount > 0;
    }

    public Task<long> CountUnreadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var unreadForUser = Builders<NotificationRecord>.Filter.And(
            Builders<NotificationRecord>.Filter.Eq(record => record.UserId, userId),
            Builders<NotificationRecord>.Filter.Eq(record => record.IsRead, false));

        return notifications.CountDocumentsAsync(unreadForUser, options: null, cancellationToken);
    }

    public async Task<Guid?> FindRecipientByOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var raisedForOrder = Builders<NotificationRecord>.Filter.Eq(record => record.OrderId, orderId);

        var earliestForOrder = await notifications
            .Find(raisedForOrder)
            .SortBy(record => record.CreatedOnUtc)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return earliestForOrder?.UserId;
    }
}
