using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Checkout.Commands;
using MassTransit;

namespace FreshCart.Ordering.Application.Checkout.Activities;

/// <summary>
/// Records the stock reservation on the aggregate and kicks off payment capture. Run as a saga
/// activity rather than inline state so the aggregate write and the next command publish are a
/// single, separately testable step. The capture command is built here because only the loaded
/// aggregate knows the settlement amount and currency; the saga state does not carry them.
/// </summary>
public sealed class MarkOrderStockReservedActivity(IOrderRepository orderRepository)
    : IStateMachineActivity<CheckoutState, StockReservedIntegrationEvent>
{
    public void Probe(ProbeContext context) => context?.CreateScope(nameof(MarkOrderStockReservedActivity));

    public void Accept(StateMachineVisitor visitor) => visitor?.Visit(this);

    public async Task Execute(
        BehaviorContext<CheckoutState, StockReservedIntegrationEvent> context,
        IBehavior<CheckoutState, StockReservedIntegrationEvent> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var orderId = context.Saga.CorrelationId;
        var reservationId = context.Message.ReservationId;

        var order = await orderRepository
            .GetByIdAsync(orderId, context.CancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Order", orderId);

        order.MarkStockReserved(reservationId);
        await orderRepository.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        context.Saga.ReservationId = reservationId;

        await context.Publish(new CaptureOrderPayment(
            order.Id,
            order.CustomerId,
            order.GrandTotal.Amount,
            order.GrandTotal.CurrencyCode,
            order.PaymentMethod)).ConfigureAwait(false);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<CheckoutState, StockReservedIntegrationEvent, TException> context,
        IBehavior<CheckoutState, StockReservedIntegrationEvent> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(next);
        return next.Faulted(context);
    }
}
