using FreshCart.Ordering.Domain.Primitives;

namespace FreshCart.Ordering.Domain.Orders.Events;

public sealed record OrderRefundedDomainEvent(
    Guid OrderId,
    Money RefundAmount,
    string Reason,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
