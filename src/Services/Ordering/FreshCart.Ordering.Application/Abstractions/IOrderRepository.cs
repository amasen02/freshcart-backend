using FreshCart.Ordering.Domain.Orders;

namespace FreshCart.Ordering.Application.Abstractions;

/// <summary>
/// Write-side persistence port for the Order aggregate. Saving changes also flushes any domain
/// events into the transactional outbox, so a single save is the atomic commit point.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the persisted status without the change tracker, so a caller recovering from a
    /// concurrency conflict observes the value the winning transaction committed rather than its own
    /// stale, tracked copy. Returns null when the order no longer exists.
    /// </summary>
    Task<OrderStatus?> GetPersistedStatusAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
