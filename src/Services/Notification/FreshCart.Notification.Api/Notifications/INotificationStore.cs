using FreshCart.BuildingBlocks.Pagination;

namespace FreshCart.Notification.Api.Notifications;

/// <summary>
/// Persistence port for notification history. The platform names Azure Cosmos DB as the production
/// sink; locally a MongoDB adapter provides the same document semantics behind this interface, so the
/// swap is a configuration concern rather than a code change.
/// </summary>
public interface INotificationStore
{
    /// <summary>
    /// Persists a notification. Returns <see cref="AddNotificationOutcome.DuplicateIgnored"/> when the
    /// (UserId, SourceEventId) pair has already been stored, so callers can make event consumption
    /// idempotent without a separate read.
    /// </summary>
    Task<AddNotificationOutcome> AddAsync(NotificationDocument notification, CancellationToken cancellationToken);

    Task<PaginatedResult<NotificationDocument>> GetForUserAsync(
        Guid userId,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken);

    Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);

    Task<long> CountUnreadAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the recipient of earlier notifications raised for an order. Events such as
    /// PaymentFailed and OrderRefunded carry only the order id (their contract omits the customer);
    /// the recipient is recovered from the OrderPlaced/OrderConfirmed entry stored earlier in the
    /// saga, which always precedes them. Returns <c>null</c> when no prior entry exists.
    /// </summary>
    Task<Guid?> FindRecipientByOrderAsync(Guid orderId, CancellationToken cancellationToken);
}
