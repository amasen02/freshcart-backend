using FreshCart.Ordering.Domain.Primitives;

namespace FreshCart.Ordering.Domain.Orders.Events;

public sealed record OrderCancelledDomainEvent(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
