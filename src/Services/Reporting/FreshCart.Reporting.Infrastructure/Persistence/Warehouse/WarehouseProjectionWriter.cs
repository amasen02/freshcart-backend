using System.Data.Common;
using Dapper;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Application.Common.Abstractions;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// Writes projected facts into the read warehouse. Each integration event becomes an UPSERT into
/// the relevant denormalised table.
/// </summary>
public sealed class WarehouseProjectionWriter(
    IWarehouseConnectionFactory warehouseConnectionFactory) : IProjectionWriter
{
    public async Task ApplyOrderConfirmedAsync(
        OrderConfirmedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using (connection.ConfigureAwait(false))
        {
            var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (transaction.ConfigureAwait(false))
            {
                try
                {
                    var netRevenue = integrationEvent.GrandTotal - integrationEvent.DiscountTotal;

                    await InsertSalesFactAsync(connection, transaction, integrationEvent, netRevenue, cancellationToken)
                        .ConfigureAwait(false);
                    await InsertSalesLineFactsAsync(connection, transaction, integrationEvent, cancellationToken)
                        .ConfigureAwait(false);
                    await UpsertCustomerLifetimeValueAsync(connection, transaction, integrationEvent.CustomerId, netRevenue, cancellationToken)
                        .ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }
        }
    }

    public async Task ApplyOrderRefundedAsync(
        OrderRefundedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using (connection.ConfigureAwait(false))
        {
            const string applyRefundSql = """
                UPDATE sales_facts
                SET    refund_total = refund_total + @RefundAmount,
                       net_revenue  = net_revenue  - @RefundAmount
                WHERE  order_id = @OrderId
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                commandText: applyRefundSql,
                parameters: new { integrationEvent.OrderId, integrationEvent.RefundAmount },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    private static Task<int> InsertSalesFactAsync(
        DbConnection connection,
        DbTransaction transaction,
        OrderConfirmedIntegrationEvent integrationEvent,
        decimal netRevenue,
        CancellationToken cancellationToken)
    {
        const string insertSalesFactSql = """
            INSERT INTO sales_facts
                (order_id, customer_id, occurred_on_utc, gross_revenue, discount_total,
                 refund_total, tax_total, shipping_total, net_revenue, payment_method)
            VALUES
                (@OrderId, @CustomerId, @OccurredOnUtc, @GrossRevenue, @DiscountTotal,
                 0, @TaxTotal, @ShippingTotal, @NetRevenue, @PaymentMethod)
            ON DUPLICATE KEY UPDATE
                gross_revenue   = VALUES(gross_revenue),
                discount_total  = VALUES(discount_total),
                tax_total       = VALUES(tax_total),
                shipping_total  = VALUES(shipping_total),
                net_revenue     = VALUES(net_revenue),
                payment_method  = VALUES(payment_method)
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            commandText: insertSalesFactSql,
            parameters: new
            {
                integrationEvent.OrderId,
                integrationEvent.CustomerId,
                OccurredOnUtc = integrationEvent.OccurredOnUtc.UtcDateTime,
                GrossRevenue = integrationEvent.GrandTotal,
                integrationEvent.DiscountTotal,
                integrationEvent.TaxTotal,
                integrationEvent.ShippingTotal,
                NetRevenue = netRevenue,
                integrationEvent.PaymentMethod,
            },
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task InsertSalesLineFactsAsync(
        DbConnection connection,
        DbTransaction transaction,
        OrderConfirmedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        const string insertSalesLineFactSql = """
            INSERT INTO sales_line_facts
                (order_id, product_sku, product_name, primary_category, quantity,
                 unit_price, net_revenue, occurred_on_utc)
            VALUES
                (@OrderId, @ProductSku, @ProductName, @PrimaryCategory, @Quantity,
                 @UnitPrice, @NetRevenue, @OccurredOnUtc)
            ON DUPLICATE KEY UPDATE
                quantity     = VALUES(quantity),
                unit_price   = VALUES(unit_price),
                net_revenue  = VALUES(net_revenue)
            """;

        foreach (var line in integrationEvent.Lines)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                commandText: insertSalesLineFactSql,
                parameters: new
                {
                    integrationEvent.OrderId,
                    line.ProductSku,
                    line.ProductName,
                    line.PrimaryCategory,
                    line.Quantity,
                    line.UnitPrice,
                    NetRevenue = line.UnitPrice * line.Quantity,
                    OccurredOnUtc = integrationEvent.OccurredOnUtc.UtcDateTime,
                },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    private static Task<int> UpsertCustomerLifetimeValueAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid customerId,
        decimal netRevenue,
        CancellationToken cancellationToken)
    {
        const string upsertCustomerLifetimeValueSql = """
            INSERT INTO customer_lifetime_value (customer_id, display_name, order_count, lifetime_value)
            VALUES (@CustomerId, '', 1, @NetRevenue)
            ON DUPLICATE KEY UPDATE
                order_count    = order_count + 1,
                lifetime_value = lifetime_value + VALUES(lifetime_value)
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            commandText: upsertCustomerLifetimeValueSql,
            parameters: new { CustomerId = customerId, NetRevenue = netRevenue },
            transaction: transaction,
            cancellationToken: cancellationToken));
    }
}
