using Dapper;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Kpis;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

public sealed class DapperCustomerReadWarehouse(IWarehouseConnectionFactory warehouseConnectionFactory)
    : ICustomerReadWarehouse
{
    public async Task<IReadOnlyList<TopEntityRanking>> GetTopCustomersByLifetimeValueAsync(
        int take,
        CancellationToken cancellationToken)
    {
        const string leaderboardSql = """
            SELECT
                ROW_NUMBER() OVER (ORDER BY lifetime_value DESC) AS `Rank`,
                customer_id      AS EntityId,
                display_name     AS DisplayName,
                lifetime_value   AS MetricValue,
                order_count      AS SecondaryCount
            FROM customer_lifetime_value
            ORDER BY lifetime_value DESC
            LIMIT @Take
            """;

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var commandDefinition = new CommandDefinition(
                commandText: leaderboardSql,
                parameters: new { Take = take },
                cancellationToken: cancellationToken);
            var rows = await connection.QueryAsync<TopEntityRanking>(commandDefinition).ConfigureAwait(false);
            return rows.ToArray();
        }
    }

    public async Task<CustomerAcquisitionSummary> GetAcquisitionSummaryAsync(
        ReportingPeriod period,
        CancellationToken cancellationToken)
    {
        const string acquisitionSql = """
            SELECT
                COUNT(CASE WHEN segment = 'new'       THEN 1 END) AS NewCustomers,
                COUNT(CASE WHEN segment = 'returning' THEN 1 END) AS ReturningCustomers,
                COUNT(CASE WHEN segment = 'churned'   THEN 1 END) AS ChurnedCustomers,
                COALESCE(AVG(lifetime_value), 0)                  AS AverageLifetimeValue
            FROM customer_segment_snapshot
            WHERE segment_on_utc >= @FromUtc AND segment_on_utc < @ToUtc
            """;

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var commandDefinition = new CommandDefinition(
                commandText: acquisitionSql,
                parameters: new { FromUtc = period.FromUtc.UtcDateTime, ToUtc = period.ToUtcExclusive.UtcDateTime },
                cancellationToken: cancellationToken);
            return await connection.QuerySingleAsync<CustomerAcquisitionSummary>(commandDefinition).ConfigureAwait(false);
        }
    }
}
