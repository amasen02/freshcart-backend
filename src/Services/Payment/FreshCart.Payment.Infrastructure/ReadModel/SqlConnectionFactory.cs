using Microsoft.Data.SqlClient;

namespace FreshCart.Payment.Infrastructure.ReadModel;

/// <summary>
/// Hands out a single open connection per DI scope so the projection write and any read inside
/// the same request share one <see cref="SqlConnection"/>.
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
