using FreshCart.Ordering.Domain.Primitives;

namespace FreshCart.Ordering.Domain.Orders.Events;

public sealed record OrderSubmittedDomainEvent(
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerDisplayName,
    Money GrandTotal,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
