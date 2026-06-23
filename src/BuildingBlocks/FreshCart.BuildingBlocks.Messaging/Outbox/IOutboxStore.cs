namespace FreshCart.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Persistence port for the transactional outbox. Each service owns its own table, but every
/// implementation honours the same contract so the <see cref="OutboxPublisher"/> background worker is
/// reusable.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Returns up to <paramref name="batchSize"/> messages that have not yet been published.
    /// Implementations should order by <c>OccurredOnUtc</c> for FIFO publication.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int batchSize, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a batch of messages as published. Must be transactional with respect to the source store.
    /// </summary>
    Task MarkAsPublishedAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken);

    /// <summary>
    /// Records a publish failure via <see cref="OutboxMessage.MarkFailed"/>, which bumps the retry attempt
    /// and stores the truncated reason, dead-lettering the message once <paramref name="maxRetryAttempts"/>
    /// is reached so a poison message is not retried forever. Implementations persist the mutated message.
    /// </summary>
    Task MarkAsFailedAsync(OutboxMessage message, string error, int maxRetryAttempts, CancellationToken cancellationToken);
}
