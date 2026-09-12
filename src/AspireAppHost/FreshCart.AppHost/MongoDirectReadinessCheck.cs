using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FreshCart.AppHost;

/// <summary>
/// Checks the authenticated Mongo endpoint without replica-set member discovery.
/// </summary>
public sealed class MongoDirectReadinessCheck(
    ReferenceExpression connectionStringExpression,
    string databaseName) : IHealthCheck
{
    private MongoClient? client;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = await connectionStringExpression
                .GetValueAsync(cancellationToken)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return HealthCheckResult.Unhealthy("Mongo connection string is unavailable.");
            }

            client ??= new MongoClient(connectionString);
            var hello = await client
                .GetDatabase(databaseName)
                .RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var setName = hello.TryGetValue("setName", out var setNameValue)
                ? setNameValue.AsString
                : null;
            var isWritablePrimary = hello.TryGetValue("isWritablePrimary", out var primaryValue)
                && primaryValue.IsBoolean
                && primaryValue.AsBoolean;

            return string.Equals(setName, "rs0", StringComparison.Ordinal) && isWritablePrimary
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Mongo replica set is not a writable rs0 primary.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Mongo direct-connection readiness failed.", exception);
        }
    }
}
