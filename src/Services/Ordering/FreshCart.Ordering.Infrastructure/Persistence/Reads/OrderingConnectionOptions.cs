namespace FreshCart.Ordering.Infrastructure.Persistence.Reads;

/// <summary>
/// Holds the SQL Server connection string used by the Dapper read side, registered as a singleton so
/// the read queries do not re-resolve it from configuration on every call.
/// </summary>
public sealed class OrderingConnectionOptions
{
    public required string ConnectionString { get; init; }
}
