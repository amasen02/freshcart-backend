namespace FreshCart.Ordering.Infrastructure.Security;

/// <summary>
/// Supplies a cached, short-lived service-to-service bearer token so the saga's outbound calls to
/// Payment (HTTP) and Inventory (gRPC) authenticate as a service principal.
/// </summary>
public interface IServiceTokenProvider
{
    ValueTask<string> GetTokenAsync(CancellationToken cancellationToken);
}
