using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Ordering.Infrastructure.Persistence;

/// <summary>
/// EF Core write-side repository for the Order aggregate. Owned navigations (lines, money totals,
/// addresses) load with the aggregate automatically, so a fetched order is always complete. Saving
/// runs the outbox interceptor, which is why a single SaveChanges is the atomic commit point.
/// </summary>
public sealed class EntityFrameworkOrderRepository(OrderingDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Orders.FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);

    public Task<bool> ExistsAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Orders.AsNoTracking().AnyAsync(order => order.Id == orderId, cancellationToken);

    public Task<OrderStatus?> GetPersistedStatusAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => (OrderStatus?)order.Status)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);
        await dbContext.Orders.AddAsync(order, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException concurrencyException)
        {
            // The RowVersion token moved between load and save: another transaction (typically the
            // checkout saga's own cancel/refund compensation) already wrote this order. Surface it as
            // the shared conflict contract so the application layer never references EF Core.
            throw new ConflictException(
                "The order was modified by another operation. Reload it and retry.",
                concurrencyException);
        }
    }
}
