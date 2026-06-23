namespace FreshCart.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Persistent record of an integration event scheduled for asynchronous publication.
/// Producers insert <see cref="OutboxMessage"/> rows in the same database transaction that writes the
/// business state change; a background <c>OutboxPublisher</c> later serialises the payload back into an
/// <see cref="Events.IntegrationEvent"/> and publishes it to the message bus.
/// </summary>
/// <remarks>
/// The producer never publishes directly to the bus from a request thread, which would create a
/// dual-write problem (the DB commit may succeed while the bus call fails). The outbox guarantees
/// at-least-once delivery; consumers must therefore be idempotent.
/// </remarks>
public sealed class OutboxMessage
{
    /// <summary>Maximum number of characters of a failure reason persisted to <see cref="Error"/>.</summary>
    public const int MaxStoredErrorLength = 512;

    /// <summary>Marker prefixed onto <see cref="Error"/> when a message is retired without publishing.</summary>
    public const string DeadLetterErrorPrefix = "DEAD-LETTERED";

    public Guid Id { get; init; } = Guid.NewGuid();

    public required string EventType { get; init; }

    public required string ContentJson { get; init; }

    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ProcessedOnUtc { get; set; }

    public string? Error { get; set; }

    public int RetryAttempt { get; set; }

    /// <summary>
    /// True once the message has been retired without ever publishing: it carries a terminal processed
    /// stamp (so the publisher no longer polls it) together with a recorded failure reason. A
    /// successfully published message also has <see cref="ProcessedOnUtc"/> set, but a null
    /// <see cref="Error"/>, which is what distinguishes the two terminal states.
    /// </summary>
    public bool IsDeadLettered => ProcessedOnUtc is not null && Error is not null;

    /// <summary>
    /// Records a publish failure: bumps <see cref="RetryAttempt"/> and stores the truncated reason. Once
    /// the attempts reach <paramref name="maxRetryAttempts"/> the message is dead-lettered — stamped
    /// processed so the publisher stops polling it — rather than being retried forever, which is how
    /// a single poison message would otherwise wedge the whole outbox. Returns <see langword="true"/> when
    /// this failure dead-lettered the message.
    /// </summary>
    public bool MarkFailed(string error, int maxRetryAttempts, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetryAttempts);

        RetryAttempt++;

        if (RetryAttempt >= maxRetryAttempts)
        {
            ProcessedOnUtc = nowUtc;
            Error = Truncate($"{DeadLetterErrorPrefix} after {RetryAttempt} attempt(s): {error}");
            return true;
        }

        Error = Truncate(error);
        return false;
    }

    private static string Truncate(string value) =>
        value.Length <= MaxStoredErrorLength ? value : value[..MaxStoredErrorLength];
}
