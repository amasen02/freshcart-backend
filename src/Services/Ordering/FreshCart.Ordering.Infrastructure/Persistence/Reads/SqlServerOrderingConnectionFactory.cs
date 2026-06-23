using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace FreshCart.Ordering.Infrastructure.Persistence.Reads;

/// <summary>
/// Opens SQL Server connections for the Dapper read projections.
/// </summary>
public sealed class SqlServerOrderingConnectionFactory(OrderingConnectionOptions options) : IOrderingConnectionFactory
{
    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("Ordering connection string is not configured.");
        }

        var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
