namespace FreshCart.BuildingBlocks.Messaging.Events;

/// <summary>
/// Base type for every cross-service integration event. Sets up the standard identification and
/// timestamp surface so consumers can deduplicate, correlate and order events without each producer
/// having to invent the same fields.
/// </summary>
/// <remarks>
/// Integration events are intentionally serialised across process boundaries. Treat their public shape
/// as a contract: additive changes only. Breaking changes require a new event type and a transition
/// window during which both versions are emitted.
/// </remarks>
public abstract record IntegrationEvent
{
    /// <summary>
    /// Stable identifier assigned by the producer. Consumers must use this for idempotency lookup.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Producer-wall-clock timestamp in UTC. Treat as informational only; do not order events by it
    /// across producers. Use <c>EventId</c> + producer position for ordering.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Fully-qualified .NET type name. Useful for diagnostics and dead-letter inspection.
    /// </summary>
    public string EventType => GetType().AssemblyQualifiedName ?? GetType().FullName ?? GetType().Name;
}
