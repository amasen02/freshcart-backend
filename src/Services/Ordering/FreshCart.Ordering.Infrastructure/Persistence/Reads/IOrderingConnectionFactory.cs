using System.Data.Common;

namespace FreshCart.Ordering.Infrastructure.Persistence.Reads;

/// <summary>
/// Opens raw connections for the Dapper read side. The read projections bypass EF Core change
/// tracking entirely, so they need a connection rather than the DbContext.
/// </summary>
public interface IOrderingConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
