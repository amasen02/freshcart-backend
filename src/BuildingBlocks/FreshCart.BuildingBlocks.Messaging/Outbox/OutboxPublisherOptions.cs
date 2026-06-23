namespace FreshCart.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Tuneable options for the <see cref="OutboxPublisher"/>. Bound from the <c>Outbox</c> configuration section.
/// </summary>
public sealed class OutboxPublisherOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; init; } = 100;

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How many publish attempts a message gets before it is dead-lettered instead of retried forever.
    /// Guards the outbox against a single poison message wedging the queue.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 5;
}
