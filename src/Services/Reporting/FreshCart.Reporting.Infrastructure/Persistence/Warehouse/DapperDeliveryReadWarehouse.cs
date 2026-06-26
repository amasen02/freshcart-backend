using Dapper;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

public sealed class DapperDeliveryReadWarehouse(IWarehouseConnectionFactory warehouseConnectionFactory)
    : IDeliveryReadWarehouse
{
    public async Task<DeliveryPerformanceSummary> GetPerformanceSummaryAsync(
        ReportingPeriod period,
        CancellationToken cancellationToken)
    {
        const string performanceSql = """
            SELECT
                COUNT(*)                                                                                  AS TotalDeliveries,
                COUNT(CASE WHEN outcome = 'on_time' THEN 1 END)                                           AS OnTimeCount,
                COUNT(CASE WHEN outcome = 'late'    THEN 1 END)                                           AS LateCount,
                COUNT(CASE WHEN outcome = 'failed'  THEN 1 END)                                           AS FailedCount,
                COALESCE(AVG(duration_minutes), 0)                                                        AS AverageDurationMinutes,
                COALESCE(SUM(CASE WHEN outcome = 'on_time' THEN 1.0 ELSE 0.0 END) / NULLIF(COUNT(*), 0), 0) * 100 AS OnTimePercentage
            FROM delivery_facts
            WHERE completed_on_utc >= @FromUtc AND completed_on_utc < @ToUtc
            """;

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var commandDefinition = new CommandDefinition(
                commandText: performanceSql,
                parameters: new { FromUtc = period.FromUtc.UtcDateTime, ToUtc = period.ToUtcExclusive.UtcDateTime },
                cancellationToken: cancellationToken);
            return await connection.QuerySingleAsync<DeliveryPerformanceSummary>(commandDefinition).ConfigureAwait(false);
        }
    }
}
