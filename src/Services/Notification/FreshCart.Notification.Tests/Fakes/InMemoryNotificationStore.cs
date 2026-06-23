using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Notification.Api.Notifications;

namespace FreshCart.Notification.Tests.Fakes;

/// <summary>
/// In-memory <see cref="INotificationStore"/> that honours the same (UserId, SourceEventId)
/// uniqueness rule the MongoDB unique index enforces, so the consumers' idempotency behaviour can be
/// asserted without a database.
/// </summary>
public sealed class InMemoryNotificationStore : INotificationStore
{
    private readonly List<NotificationDocument> stored = [];

    public IReadOnlyList<NotificationDocument> Stored => stored;

    /// <summary>Seeds a pre-existing notification, used to model an earlier event in the saga flow.</summary>
    public void Seed(NotificationDocument notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        stored.Add(notification);
    }

    public Task<AddNotificationOutcome> AddAsync(NotificationDocument notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var alreadyStored = stored.Any(existing =>
            existing.UserId == notification.UserId && existing.SourceEventId == notification.SourceEventId);

        if (alreadyStored)
        {
            return Task.FromResult(AddNotificationOutcome.DuplicateIgnored);
        }

        stored.Add(notification);
        return Task.FromResult(AddNotificationOutcome.Stored);
    }

    public Task<PaginatedResult<NotificationDocument>> GetForUserAsync(
        Guid userId,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var normalisedRequest = paginationRequest.Normalise();
        var ownedByUser = stored
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedOnUtc)
            .ToList();

        var page = ownedByUser
            .Skip((normalisedRequest.PageNumber - 1) * normalisedRequest.PageSize)
            .Take(normalisedRequest.PageSize)
            .ToArray();

        return Task.FromResult(new PaginatedResult<NotificationDocument>(
            normalisedRequest.PageNumber,
            normalisedRequest.PageSize,
            ownedByUser.Count,
            page));
    }

    public Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var index = stored.FindIndex(notification =>
            notification.Id == notificationId && notification.UserId == userId);

        if (index < 0 || stored[index].IsRead)
        {
            return Task.FromResult(false);
        }

        var current = stored[index];
        stored[index] = new NotificationDocument
        {
            Id = current.Id,
            UserId = current.UserId,
            SourceEventId = current.SourceEventId,
            Type = current.Type,
            Title = current.Title,
            Message = current.Message,
            OrderId = current.OrderId,
            CreatedOnUtc = current.CreatedOnUtc,
            IsRead = true,
        };

        return Task.FromResult(true);
    }

    public Task<long> CountUnreadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var unread = stored.LongCount(notification => notification.UserId == userId && !notification.IsRead);
        return Task.FromResult(unread);
    }

    public Task<Guid?> FindRecipientByOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var earliest = stored
            .Where(notification => notification.OrderId == orderId)
            .OrderBy(notification => notification.CreatedOnUtc)
            .FirstOrDefault();

        return Task.FromResult(earliest?.UserId);
    }
}
