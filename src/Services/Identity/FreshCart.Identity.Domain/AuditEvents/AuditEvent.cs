namespace FreshCart.Identity.Domain.AuditEvents;

/// <summary>
/// Append-only record of security-relevant activity within the Identity service.
/// Mirrors the OWASP A09 (Security Logging) recommendation: persist every authentication event,
/// every role change, every password reset, every MFA enable/disable so an investigator can
/// reconstruct what happened months after the fact.
/// </summary>
public sealed class AuditEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string EventType { get; init; }

    public Guid? UserId { get; init; }

    public required string Description { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? CorrelationId { get; init; }
}
