using System.Data.Common;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// Hands out an open <see cref="DbConnection"/> bound to the warehouse database. Centralises the
/// retry-on-failure policy and connection-string resolution.
/// </summary>
public interface IWarehouseConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
