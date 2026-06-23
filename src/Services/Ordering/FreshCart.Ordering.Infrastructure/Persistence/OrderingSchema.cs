namespace FreshCart.Ordering.Infrastructure.Persistence;

/// <summary>
/// Database schema name shared by every Ordering table so the bounded context owns a clear
/// namespace inside the SQL Server database.
/// </summary>
public static class OrderingSchema
{
    public const string Name = "ordering";
}
