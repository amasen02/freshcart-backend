using Microsoft.Data.SqlClient;

namespace FreshCart.Inventory.Api.Repositories;

public interface ISqlConnectionFactory : IAsyncDisposable
{
    Task<SqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken);
}
