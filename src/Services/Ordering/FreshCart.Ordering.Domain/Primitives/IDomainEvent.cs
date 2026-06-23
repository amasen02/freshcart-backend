namespace FreshCart.Ordering.Domain.Primitives;

/// <summary>
/// Marker for events raised inside the aggregate boundary. Domain events stay in process; the
/// outbox interceptor translates them into integration events in the same database transaction.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}
