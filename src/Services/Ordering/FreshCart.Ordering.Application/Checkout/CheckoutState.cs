using MassTransit;

namespace FreshCart.Ordering.Application.Checkout;

/// <summary>
/// Persistent state of one checkout orchestration; the correlation id is the order id assigned
/// at basket checkout. RowVersion is the optimistic concurrency token used by the EF saga store.
/// </summary>
public sealed class CheckoutState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public Guid? ReservationId { get; set; }

    public Guid? PaymentId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
