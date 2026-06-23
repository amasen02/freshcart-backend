using Dapper;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Kpis;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

public sealed class DapperProductReadWarehouse(IWarehouseConnectionFactory warehouseConnectionFactory)
    : IProductReadWarehouse
{
    public async Task<IReadOnlyList<TopEntityRanking>> GetTopSellingProductsAsync(
        ReportingPeriod period,
        int take,
        CancellationToken cancellationToken)
    {
        const string topSellersSql = """
            SELECT
                ROW_NUMBER() OVER (ORDER BY SUM(quantity) DESC) AS `Rank`,
                product_sku                          AS EntityId,
                MAX(product_name)                    AS DisplayName,
                COALESCE(SUM(net_revenue), 0)        AS MetricValue,
                COALESCE(SUM(quantity), 0)           AS SecondaryCount
            FROM sales_line_facts
            WHERE occurred_on_utc >= @FromUtc AND occurred_on_utc < @ToUtc
            GROUP BY product_sku
            ORDER BY SecondaryCount DESC
            LIMIT @Take
            """;

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var commandDefinition = new CommandDefinition(
                commandText: topSellersSql,
                parameters: new { FromUtc = period.FromUtc.UtcDateTime, ToUtc = period.ToUtcExclusive.UtcDateTime, Take = take },
                cancellationToken: cancellationToken);
            var rows = await connection.QueryAsync<TopEntityRanking>(commandDefinition).ConfigureAwait(false);
            return rows.ToArray();
        }
    }

    public async Task<IReadOnlyList<TopEntityRanking>> GetSlowMovingProductsAsync(
        ReportingPeriod period,
        int take,
        CancellationToken cancellationToken)
    {
        const string slowMoversSql = """
            SELECT
                ROW_NUMBER() OVER (ORDER BY SUM(quantity) ASC) AS `Rank`,
                product_sku                          AS EntityId,
                MAX(product_name)                    AS DisplayName,
                COALESCE(SUM(net_revenue), 0)        AS MetricValue,
                COALESCE(SUM(quantity), 0)           AS SecondaryCount
            FROM sales_line_facts
            WHERE occurred_on_utc >= @FromUtc AND occurred_on_utc < @ToUtc
            GROUP BY product_sku
            HAVING SUM(quantity) > 0
            ORDER BY SecondaryCount ASC
            LIMIT @Take
            """;

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var commandDefinition = new CommandDefinition(
                commandText: slowMoversSql,
                parameters: new { FromUtc = period.FromUtc.UtcDateTime, ToUtc = period.ToUtcExclusive.UtcDateTime, Take = take },
                cancellationToken: cancellationToken);
            var rows = await connection.QueryAsync<TopEntityRanking>(commandDefinition).ConfigureAwait(false);
            return rows.ToArray();
        }
    }

    public async Task<InventoryHealthSummary> GetInventoryHealthAsync(CancellationToken cancellationToken)
    {
        const string inventoryHealthSql = """
            SELECT
                COUNT(*)                                              AS TotalSkus,
                COUNT(CASE WHEN on_hand <= 0 THEN 1 END)              AS OutOfStockCount,
                COUNT(CASE WHEN on_hand > 0 AND on_hand <= reorder_threshold THEN 1 END) AS LowStockCount,
                COUNT(CASE WHEN on_hand >= overstock_threshold THEN 1 END) AS OverstockCount,
                COALESCE(SUM(on_hand * unit_cost), 0)                 AS InventoryValueAtCost
            FROM inventory_snapshot
            """;

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var commandDefinition = new CommandDefinition(commandText: inventoryHealthSql, cancellationToken: cancellationToken);
            return await connection.QuerySingleAsync<InventoryHealthSummary>(commandDefinition).ConfigureAwait(false);
        }
    }
}
