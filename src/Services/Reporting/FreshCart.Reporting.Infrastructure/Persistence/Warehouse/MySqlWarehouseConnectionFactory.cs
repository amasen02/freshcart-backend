using System.Data.Common;
using MySqlConnector;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

public sealed class MySqlWarehouseConnectionFactory(WarehouseConnectionOptions options) : IWarehouseConnectionFactory
{
    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("Warehouse connection string is not configured.");
        }

        var mysqlConnection = new MySqlConnection(options.ConnectionString);
        await mysqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return mysqlConnection;
    }
}
