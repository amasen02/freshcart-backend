using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Abstractions;
using MassTransit;

namespace FreshCart.Ordering.Application.Checkout.Activities;

/// <summary>
/// Cancels the order when stock could not be reserved. There is no reservation to release because
/// the failure means none was taken. Cancelling raises the domain event that the outbox turns into
/// OrderCancelled so Inventory and Notification learn of the outcome.
/// </summary>
public sealed class CancelOrderOnStockFailureActivity(IOrderRepository orderRepository, TimeProvider timeProvider)
    : IStateMachineActivity<CheckoutState, StockReservationFailedIntegrationEvent>
{
    public void Probe(ProbeContext context) => context?.CreateScope(nameof(CancelOrderOnStockFailureActivity));

    public void Accept(StateMachineVisitor visitor) => visitor?.Visit(this);

    public async Task Execute(
        BehaviorContext<CheckoutState, StockReservationFailedIntegrationEvent> context,
        IBehavior<CheckoutState, StockReservationFailedIntegrationEvent> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var orderId = context.Saga.CorrelationId;

        var order = await orderRepository
            .GetByIdAsync(orderId, context.CancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Order", orderId);

        order.Cancel(context.Message.Reason, timeProvider.GetUtcNow());
        await orderRepository.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<CheckoutState, StockReservationFailedIntegrationEvent, TException> context,
        IBehavior<CheckoutState, StockReservationFailedIntegrationEvent> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(next);
        return next.Faulted(context);
    }
}
