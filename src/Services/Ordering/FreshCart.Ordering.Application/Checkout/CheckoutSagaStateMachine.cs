using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Checkout.Activities;
using FreshCart.Ordering.Application.Checkout.Commands;
using MassTransit;

namespace FreshCart.Ordering.Application.Checkout;

/// <summary>
/// Orchestrates checkout: reserve stock, capture payment, confirm. The state machine stays
/// declarative; aggregate updates run in activities and external calls run in work consumers
/// driven by saga-internal commands, so each side effect is testable on its own.
/// </summary>
public sealed class CheckoutSagaStateMachine : MassTransitStateMachine<CheckoutState>
{
    public CheckoutSagaStateMachine()
    {
        InstanceState(instance => instance.CurrentState);

        Event(() => CheckoutStarted, eventConfigurator => eventConfigurator.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReserved, eventConfigurator => eventConfigurator.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReservationFailed, eventConfigurator => eventConfigurator.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentCaptured, eventConfigurator => eventConfigurator.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentFailed, eventConfigurator => eventConfigurator.CorrelateById(context => context.Message.OrderId));

        Initially(
            When(CheckoutStarted)
                .Then(context => context.Saga.CustomerId = context.Message.CustomerId)
                .Publish(context => new SubmitOrderFromCheckout(context.Message))
                .Publish(context => new ReserveOrderStock(context.Message.OrderId, context.Message.Lines))
                .TransitionTo(AwaitingStockReservation));

        During(AwaitingStockReservation,
            When(StockReserved)
                .Activity(activitySelector => activitySelector.OfType<MarkOrderStockReservedActivity>())
                .TransitionTo(AwaitingPayment),
            When(StockReservationFailed)
                .Activity(activitySelector => activitySelector.OfType<CancelOrderOnStockFailureActivity>())
                .TransitionTo(Cancelled)
                .Finalize());

        During(AwaitingPayment,
            When(PaymentCaptured)
                .Activity(activitySelector => activitySelector.OfType<ConfirmOrderActivity>())
                .TransitionTo(Confirmed)
                .Finalize(),
            When(PaymentFailed)
                .Activity(activitySelector => activitySelector.OfType<CancelOrderOnPaymentFailureActivity>())
                .TransitionTo(Cancelled)
                .Finalize());

        SetCompletedWhenFinalized();
    }

    public State AwaitingStockReservation { get; private set; } = null!;

    public State AwaitingPayment { get; private set; } = null!;

    public State Confirmed { get; private set; } = null!;

    public State Cancelled { get; private set; } = null!;

    public Event<BasketCheckoutStartedIntegrationEvent> CheckoutStarted { get; private set; } = null!;

    public Event<StockReservedIntegrationEvent> StockReserved { get; private set; } = null!;

    public Event<StockReservationFailedIntegrationEvent> StockReservationFailed { get; private set; } = null!;

    public Event<PaymentCapturedIntegrationEvent> PaymentCaptured { get; private set; } = null!;

    public Event<PaymentFailedIntegrationEvent> PaymentFailed { get; private set; } = null!;
}
