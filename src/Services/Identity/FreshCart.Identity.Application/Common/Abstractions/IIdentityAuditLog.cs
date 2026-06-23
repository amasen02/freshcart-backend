using FreshCart.Identity.Domain.AuditEvents;

namespace FreshCart.Identity.Application.Common.Abstractions;

/// <summary>
/// Append-only audit sink. Infrastructure persists the event; application code never mutates one
/// after creation.
/// </summary>
public interface IIdentityAuditLog
{
    Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
