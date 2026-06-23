namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// Resolved warehouse connection settings handed to the connection factory.
/// </summary>
public sealed class WarehouseConnectionOptions
{
    public const string SectionName = "Warehouse";

    public required string ConnectionString { get; init; }
}
