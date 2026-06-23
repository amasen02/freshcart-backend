namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Idempotency-key store for projection consumers. Message brokers deliver at-least-once, so every
/// consumer checks the inbox before applying state changes and records the event id afterwards.
/// </summary>
public interface IProjectionInbox
{
    Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken);

    Task RecordProcessedAsync(Guid eventId, CancellationToken cancellationToken);
}
