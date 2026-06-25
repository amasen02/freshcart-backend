using FreshCart.Payment.Domain.Events;

namespace FreshCart.Payment.Application.Abstractions;

/// <summary>
/// Append-only store for payment event streams. <c>expectedVersion</c> is the stream version the
/// caller observed before raising the new events; the implementation must reject the append with
/// a conflict when another writer has advanced the stream past it. The append also stages a projection
/// intent in the same transaction so the SQL read model is projected exactly-once, asynchronously.
/// </summary>
public interface IPaymentEventStore
{
    Task AppendAsync(
        Guid orderId,
        Guid paymentId,
        int expectedVersion,
        IReadOnlyList<IPaymentEvent> newEvents,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IPaymentEvent>> LoadStreamAsync(Guid paymentId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the payment stream id for an order, or <see langword="null"/> if none exists. This is the
    /// idempotency check for capture: it reads the event store (the source of truth) rather than the
    /// asynchronously-projected read model, so a not-yet-projected payment cannot be captured twice.
    /// </summary>
    Task<Guid?> FindStreamIdByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
}
