using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Abstractions;
using MassTransit;

namespace FreshCart.Ordering.Application.Checkout.Activities;

/// <summary>
/// Compensates a payment failure: the stock was already reserved when we reached AwaitingPayment, so
/// the reservation is released before the order is cancelled. Releasing first means the order is
/// never left cancelled while still holding stock. Cancelling raises the domain event that the
/// outbox turns into OrderCancelled.
/// </summary>
public sealed class CancelOrderOnPaymentFailureActivity(
    IOrderRepository orderRepository,
    IInventoryClient inventoryClient,
    TimeProvider timeProvider)
    : IStateMachineActivity<CheckoutState, PaymentFailedIntegrationEvent>
{
    public void Probe(ProbeContext context) => context?.CreateScope(nameof(CancelOrderOnPaymentFailureActivity));

    public void Accept(StateMachineVisitor visitor) => visitor?.Visit(this);

    public async Task Execute(
        BehaviorContext<CheckoutState, PaymentFailedIntegrationEvent> context,
        IBehavior<CheckoutState, PaymentFailedIntegrationEvent> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var orderId = context.Saga.CorrelationId;

        var order = await orderRepository
            .GetByIdAsync(orderId, context.CancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Order", orderId);

        if (order.ReservationId.HasValue)
        {
            await inventoryClient.ReleaseReservationAsync(orderId, context.CancellationToken).ConfigureAwait(false);
        }

        order.Cancel(context.Message.Reason, timeProvider.GetUtcNow());
        await orderRepository.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<CheckoutState, PaymentFailedIntegrationEvent, TException> context,
        IBehavior<CheckoutState, PaymentFailedIntegrationEvent> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(next);
        return next.Faulted(context);
    }
}
