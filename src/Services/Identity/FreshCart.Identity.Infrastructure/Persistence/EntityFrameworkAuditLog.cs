using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.AuditEvents;

namespace FreshCart.Identity.Infrastructure.Persistence;

/// <summary>
/// <see cref="IIdentityAuditLog"/> implementation that persists each event to the
/// <c>identity.AuditEvents</c> table via EF Core. The table is append-only: there is no Update or
/// Delete in this class by design.
/// </summary>
public sealed class EntityFrameworkAuditLog(IdentityDbContext identityDbContext) : IIdentityAuditLog
{
    public Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        identityDbContext.AuditEvents.Add(auditEvent);
        return identityDbContext.SaveChangesAsync(cancellationToken);
    }
}
