using FreshCart.Payment.Domain.Events;

namespace FreshCart.Payment.Application.Abstractions;

/// <summary>
/// Append-only store for payment event streams. <c>expectedVersion</c> is the stream version the
/// caller observed before raising the new events; the implementation must reject the append with
/// a conflict when another writer has advanced the stream past it.
/// </summary>
public interface IPaymentEventStore
{
    Task AppendAsync(
        Guid paymentId,
        int expectedVersion,
        IReadOnlyList<IPaymentEvent> newEvents,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IPaymentEvent>> LoadStreamAsync(Guid paymentId, CancellationToken cancellationToken);
}
