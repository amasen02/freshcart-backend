using Dapper;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Sales;
using Microsoft.Extensions.Logging;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// Dapper-backed implementation of the sales read warehouse. Queries are hand-tuned set-based
/// statements against denormalised tables that the projection writer maintains.
/// </summary>
public sealed class DapperSalesReadWarehouse(
    IWarehouseConnectionFactory warehouseConnectionFactory,
    ILogger<DapperSalesReadWarehouse> logger) : ISalesReadWarehouse
{
    public async Task<SalesSnapshot> GetAggregateAsync(ReportingPeriod period, CancellationToken cancellationToken)
    {
        const string aggregateSql = """
            SELECT
                DATE(MIN(occurred_on_utc)) AS Day,
                COUNT(DISTINCT order_id)   AS OrderCount,
                COUNT(DISTINCT customer_id) AS UniqueCustomerCount,
                COALESCE(SUM(gross_revenue),  0) AS GrossRevenue,
                COALESCE(SUM(discount_total), 0) AS DiscountTotal,
                COALESCE(SUM(refund_total),   0) AS RefundTotal,
                COALESCE(SUM(tax_total),      0) AS TaxTotal,
                COALESCE(SUM(shipping_total), 0) AS ShippingTotal,
                COALESCE(SUM(net_revenue),    0) AS NetRevenue
            FROM sales_facts
            WHERE occurred_on_utc >= @FromUtc AND occurred_on_utc < @ToUtc
            """;

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {

            var commandDefinition = new CommandDefinition(
                commandText: aggregateSql,
                parameters: new { FromUtc = period.FromUtc.UtcDateTime, ToUtc = period.ToUtcExclusive.UtcDateTime },
                cancellationToken: cancellationToken);

            var aggregateRow = await connection
                .QuerySingleOrDefaultAsync<SalesSnapshot?>(commandDefinition)
                .ConfigureAwait(false);

            return aggregateRow ?? new SalesSnapshot(
                Day: DateOnly.FromDateTime(period.FromUtc.UtcDateTime),
                OrderCount: 0,
                UniqueCustomerCount: 0,
                GrossRevenue: 0,
                DiscountTotal: 0,
                RefundTotal: 0,
                TaxTotal: 0,
                ShippingTotal: 0,
                NetRevenue: 0);
        }
    }

    public async Task<IReadOnlyList<SalesSnapshot>> GetTimeSeriesAsync(
        ReportingPeriod period,
        AggregationBucket bucket,
        CancellationToken cancellationToken)
    {
        var (bucketExpression, orderClause) = bucket switch
        {
            AggregationBucket.Hourly  => ("DATE_FORMAT(occurred_on_utc, '%Y-%m-%d %H:00:00')", "1 ASC"),
            AggregationBucket.Daily   => ("DATE(occurred_on_utc)",                              "1 ASC"),
            AggregationBucket.Weekly  => ("DATE(DATE_SUB(occurred_on_utc, INTERVAL WEEKDAY(occurred_on_utc) DAY))", "1 ASC"),
            AggregationBucket.Monthly => ("DATE_FORMAT(occurred_on_utc, '%Y-%m-01')",           "1 ASC"),
            _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, "Unsupported bucket size."),
        };

        var timeSeriesSql = $"""
            SELECT
                {bucketExpression}            AS Day,
                COUNT(DISTINCT order_id)      AS OrderCount,
                COUNT(DISTINCT customer_id)   AS UniqueCustomerCount,
                COALESCE(SUM(gross_revenue),  0) AS GrossRevenue,
                COALESCE(SUM(discount_total), 0) AS DiscountTotal,
                COALESCE(SUM(refund_total),   0) AS RefundTotal,
                COALESCE(SUM(tax_total),      0) AS TaxTotal,
                COALESCE(SUM(shipping_total), 0) AS ShippingTotal,
                COALESCE(SUM(net_revenue),    0) AS NetRevenue
            FROM sales_facts
            WHERE occurred_on_utc >= @FromUtc AND occurred_on_utc < @ToUtc
            GROUP BY {bucketExpression}
            ORDER BY {orderClause}
            """;

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {

            var commandDefinition = new CommandDefinition(
                commandText: timeSeriesSql,
                parameters: new { FromUtc = period.FromUtc.UtcDateTime, ToUtc = period.ToUtcExclusive.UtcDateTime },
                cancellationToken: cancellationToken);

            var rows = await connection.QueryAsync<SalesSnapshot>(commandDefinition).ConfigureAwait(false);
            var materialisedRows = rows.ToArray();

            logger.LogDebug("Sales time-series returned {RowCount} buckets for period {Period}", materialisedRows.Length, period);

            return materialisedRows;
        }
    }

    public async Task<IReadOnlyList<RevenueByCategoryRow>> GetRevenueByCategoryAsync(
        ReportingPeriod period,
        CancellationToken cancellationToken)
    {
        const string byCategorySql = """
            SELECT
                primary_category              AS CategoryName,
                COUNT(DISTINCT order_id)      AS OrderCount,
                COALESCE(SUM(net_revenue), 0) AS NetRevenue
            FROM sales_line_facts
            WHERE occurred_on_utc >= @FromUtc AND occurred_on_utc < @ToUtc
            GROUP BY primary_category
            ORDER BY NetRevenue DESC
            """;

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {

            var commandDefinition = new CommandDefinition(
                commandText: byCategorySql,
                parameters: new { FromUtc = period.FromUtc.UtcDateTime, ToUtc = period.ToUtcExclusive.UtcDateTime },
                cancellationToken: cancellationToken);

            var rows = await connection.QueryAsync<RevenueByCategoryRow>(commandDefinition).ConfigureAwait(false);
            return rows.ToArray();
        }
    }

    public async Task<IReadOnlyList<RevenueByPaymentMethodRow>> GetRevenueByPaymentMethodAsync(
        ReportingPeriod period,
        CancellationToken cancellationToken)
    {
        const string byPaymentMethodSql = """
            SELECT
                payment_method                AS PaymentMethod,
                COUNT(*)                      AS TransactionCount,
                COALESCE(SUM(net_revenue), 0) AS NetRevenue
            FROM sales_facts
            WHERE occurred_on_utc >= @FromUtc AND occurred_on_utc < @ToUtc
            GROUP BY payment_method
            ORDER BY NetRevenue DESC
            """;

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {

            var commandDefinition = new CommandDefinition(
                commandText: byPaymentMethodSql,
                parameters: new { FromUtc = period.FromUtc.UtcDateTime, ToUtc = period.ToUtcExclusive.UtcDateTime },
                cancellationToken: cancellationToken);

            var rows = await connection.QueryAsync<RevenueByPaymentMethodRow>(commandDefinition).ConfigureAwait(false);
            return rows.ToArray();
        }
    }
}
