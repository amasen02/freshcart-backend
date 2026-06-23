using Microsoft.Data.SqlClient;

namespace FreshCart.Inventory.Api.Repositories;

/// <summary>
/// Hands out a single open connection per DI scope so the reservation transaction and every
/// repository call inside it share the same <see cref="SqlConnection"/>.
/// </summary>
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;
    private SqlConnection? _scopedConnection;

    public SqlConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _connectionString = connectionString;
    }

    public async Task<SqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_scopedConnection is null)
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            _scopedConnection = connection;
        }

        return _scopedConnection;
    }

    public async ValueTask DisposeAsync()
    {
        if (_scopedConnection is not null)
        {
            await _scopedConnection.DisposeAsync().ConfigureAwait(false);
            _scopedConnection = null;
        }
    }
}
