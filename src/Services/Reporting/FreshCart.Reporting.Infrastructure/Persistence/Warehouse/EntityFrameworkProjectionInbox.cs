using FreshCart.Reporting.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

public sealed class EntityFrameworkProjectionInbox(
    WarehouseDbContext warehouseDbContext,
    TimeProvider timeProvider) : IProjectionInbox
{
    public Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken)
        => warehouseDbContext.ProjectionInbox.AsNoTracking()
            .AnyAsync(entry => entry.EventId == eventId, cancellationToken);

    public Task RecordProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        warehouseDbContext.ProjectionInbox.Add(new ProjectionInboxEntry
        {
            EventId = eventId,
            ProcessedOnUtc = timeProvider.GetUtcNow(),
        });
        return warehouseDbContext.SaveChangesAsync(cancellationToken);
    }
}
