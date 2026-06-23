using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Abstractions;
using MassTransit;

namespace FreshCart.Ordering.Application.Checkout.Activities;

/// <summary>
/// Marks the order paid and confirms it. The confirmation raises the domain event that the outbox
/// interceptor turns into OrderConfirmed in the same transaction, so downstream services see the
/// confirmation only once the aggregate is durably confirmed.
/// </summary>
public sealed class ConfirmOrderActivity(IOrderRepository orderRepository, TimeProvider timeProvider)
    : IStateMachineActivity<CheckoutState, PaymentCapturedIntegrationEvent>
{
    public void Probe(ProbeContext context) => context?.CreateScope(nameof(ConfirmOrderActivity));

    public void Accept(StateMachineVisitor visitor) => visitor?.Visit(this);

    public async Task Execute(
        BehaviorContext<CheckoutState, PaymentCapturedIntegrationEvent> context,
        IBehavior<CheckoutState, PaymentCapturedIntegrationEvent> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var orderId = context.Saga.CorrelationId;
        var paymentId = context.Message.PaymentId;

        var order = await orderRepository
            .GetByIdAsync(orderId, context.CancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Order", orderId);

        order.MarkPaid(paymentId);
        order.Confirm(timeProvider.GetUtcNow());
        await orderRepository.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        context.Saga.PaymentId = paymentId;

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<CheckoutState, PaymentCapturedIntegrationEvent, TException> context,
        IBehavior<CheckoutState, PaymentCapturedIntegrationEvent> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(next);
        return next.Faulted(context);
    }
}
