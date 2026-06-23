using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FreshCart.Pricing.Grpc.Persistence;

/// <summary>
/// Readiness probe for the embedded SQLite store. There is no AspNetCore.HealthChecks package
/// for SQLite, so the check asks the scoped <see cref="PricingDbContext"/> directly.
/// </summary>
public sealed class SqliteDatabaseHealthCheck(PricingDbContext pricingDbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var databaseIsReachable = await pricingDbContext.Database
            .CanConnectAsync(cancellationToken)
            .ConfigureAwait(false);

        return databaseIsReachable
            ? HealthCheckResult.Healthy("The pricing SQLite database is reachable.")
            : HealthCheckResult.Unhealthy("The pricing SQLite database is unreachable.");
    }
}
