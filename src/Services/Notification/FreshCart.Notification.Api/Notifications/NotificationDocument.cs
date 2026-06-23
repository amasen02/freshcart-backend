namespace FreshCart.Notification.Api.Notifications;

/// <summary>
/// A single notification kept in the per-user history. The <see cref="SourceEventId"/> is the
/// <c>EventId</c> of the integration event that produced this entry; together with
/// <see cref="UserId"/> it forms the idempotency key the store enforces with a unique index, so a
/// redelivered integration event never creates a duplicate row.
/// </summary>
public sealed class NotificationDocument
{
    public required Guid Id { get; init; }

    public required Guid UserId { get; init; }

    public required Guid SourceEventId { get; init; }

    public required string Type { get; init; }

    public required string Title { get; init; }

    public required string Message { get; init; }

    public Guid? OrderId { get; init; }

    public required DateTimeOffset CreatedOnUtc { get; init; }

    public bool IsRead { get; init; }
}
