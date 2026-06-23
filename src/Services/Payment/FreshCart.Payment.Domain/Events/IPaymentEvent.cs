namespace FreshCart.Payment.Domain.Events;

/// <summary>
/// Contract every payment event satisfies. <see cref="Version"/> is the position of the event in
/// its stream (1-based); the event store enforces uniqueness of (PaymentId, Version) so concurrent
/// writers cannot fork a stream.
/// </summary>
public interface IPaymentEvent
{
    Guid PaymentId { get; }

    int Version { get; }

    DateTimeOffset OccurredOnUtc { get; }
}
