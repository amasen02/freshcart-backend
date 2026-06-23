using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Domain.Orders;
using MediatR;

namespace FreshCart.Ordering.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IInventoryClient inventoryClient,
    TimeProvider timeProvider) : ICommandHandler<CancelOrderCommand>
{
    public async Task<Unit> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await orderRepository
            .GetByIdAsync(command.OrderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Order", command.OrderId);

        if (order.CustomerId != command.RequestingCustomerId)
        {
            throw new ForbiddenException("Only the owning customer may cancel this order.");
        }

        order.Cancel(command.Reason, timeProvider.GetUtcNow());

        if (order.ReservationId.HasValue)
        {
            await inventoryClient.ReleaseReservationAsync(order.Id, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConflictException)
        {
            // The RowVersion race was lost. If the winning transaction (the checkout saga's own
            // compensation) already cancelled this order, the customer's intent is satisfied and the
            // duplicate OrderCancelled event was never written, so this is an idempotent success.
            // Any other persisted state means a genuinely conflicting transition, which must surface.
            var persistedStatus = await orderRepository
                .GetPersistedStatusAsync(command.OrderId, cancellationToken)
                .ConfigureAwait(false);

            if (persistedStatus is not OrderStatus.Cancelled)
            {
                throw;
            }
        }

        return Unit.Value;
    }
}
