namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// EF Core row shape for the consumer idempotency inbox.
/// </summary>
public sealed class ProjectionInboxEntry
{
    public Guid EventId { get; set; }
    public DateTimeOffset ProcessedOnUtc { get; set; }
}
