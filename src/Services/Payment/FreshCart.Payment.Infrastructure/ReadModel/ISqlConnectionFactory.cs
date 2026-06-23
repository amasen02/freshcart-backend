using Microsoft.Data.SqlClient;

namespace FreshCart.Payment.Infrastructure.ReadModel;

public interface ISqlConnectionFactory : IAsyncDisposable
{
    Task<SqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken);
}
