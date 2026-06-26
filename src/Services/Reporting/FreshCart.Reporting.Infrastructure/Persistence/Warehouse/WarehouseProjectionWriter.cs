using System.Data.Common;
using Dapper;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Application.Common.Abstractions;
using MySqlConnector;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// Writes projected facts into the read warehouse. Each integration event becomes an UPSERT into
/// the relevant denormalised table, applied exactly once: the idempotency record and the projection
/// share one transaction, so a redelivered event can never double-count the additive aggregates
/// (refund totals, customer lifetime value).
/// </summary>
public sealed class WarehouseProjectionWriter(
    IWarehouseConnectionFactory warehouseConnectionFactory) : IProjectionWriter
{
    private static readonly int DuplicateEntryErrorNumber = (int)MySqlErrorCode.DuplicateKeyEntry;

    private const string NewCustomerSegment = "new";
    private const string ReturningCustomerSegment = "returning";

    // ProductCreated carries no stock thresholds, so the snapshot seeds sensible defaults; the dashboard's
    // low-stock and overstock flags then track movement once an inventory feed supplies real thresholds.
    private const int DefaultReorderThreshold = 10;
    private const int DefaultOverstockThreshold = 1000;

    public Task<bool> ApplyOrderConfirmedAsync(
        OrderConfirmedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var netRevenue = integrationEvent.GrandTotal - integrationEvent.DiscountTotal;

        return ProjectExactlyOnceAsync(
            integrationEvent.EventId,
            async (connection, transaction, ct) =>
            {
                await InsertSalesFactAsync(connection, transaction, integrationEvent, netRevenue, ct).ConfigureAwait(false);
                await InsertSalesLineFactsAsync(connection, transaction, integrationEvent, ct).ConfigureAwait(false);

                // The segment is decided from the customer's prior lifetime-value row read inside this same
                // transaction, before the upsert advances it: no prior row means this is their first order.
                var priorLifetimeValue = await ReadCustomerLifetimeValueAsync(connection, transaction, integrationEvent.CustomerId, ct).ConfigureAwait(false);
                await UpsertCustomerLifetimeValueAsync(connection, transaction, integrationEvent.CustomerId, netRevenue, ct).ConfigureAwait(false);

                var segment = priorLifetimeValue is null ? NewCustomerSegment : ReturningCustomerSegment;
                var lifetimeValue = (priorLifetimeValue ?? 0m) + netRevenue;
                await UpsertCustomerSegmentAsync(
                        connection, transaction, integrationEvent.CustomerId, segment,
                        integrationEvent.OccurredOnUtc.UtcDateTime, lifetimeValue, ct)
                    .ConfigureAwait(false);
            },
            cancellationToken);
    }

    public Task<bool> ApplyProductCreatedAsync(
        ProductCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return ProjectExactlyOnceAsync(
            integrationEvent.EventId,
            (connection, transaction, ct) => UpsertInventorySnapshotAsync(connection, transaction, integrationEvent, ct),
            cancellationToken);
    }

    public Task<bool> ApplyDeliveryScheduledAsync(
        DeliveryScheduledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return ProjectExactlyOnceAsync(
            integrationEvent.EventId,
            (connection, transaction, ct) => InsertScheduledDeliveryFactAsync(connection, transaction, integrationEvent, ct),
            cancellationToken);
    }

    public Task<bool> ApplyDeliveryCompletedAsync(
        DeliveryCompletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return ProjectExactlyOnceAsync(
            integrationEvent.EventId,
            (connection, transaction, ct) => CompleteDeliveryFactAsync(connection, transaction, integrationEvent, ct),
            cancellationToken);
    }

    public Task<bool> ApplyOrderRefundedAsync(
        OrderRefundedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return ProjectExactlyOnceAsync(
            integrationEvent.EventId,
            (connection, transaction, ct) => ApplyRefundAsync(connection, transaction, integrationEvent, ct),
            cancellationToken);
    }

    private async Task<bool> ProjectExactlyOnceAsync(
        Guid eventId,
        Func<DbConnection, DbTransaction, CancellationToken, Task> applyProjection,
        CancellationToken cancellationToken)
    {
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
                    // The inbox insert is the idempotency latch: its primary key rejects a redelivered
                    // event, and because it shares this transaction with the projection it commits or
                    // rolls back as one unit — a crash between the two can never double-apply.
                    await InsertInboxEntryAsync(connection, transaction, eventId, cancellationToken).ConfigureAwait(false);
                }
                catch (MySqlException duplicate) when (duplicate.Number == DuplicateEntryErrorNumber)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return false;
                }

                try
                {
                    await applyProjection(connection, transaction, cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }
        }
    }

    private static Task<int> InsertInboxEntryAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        const string insertInboxSql = """
            INSERT INTO projection_inbox (EventId, ProcessedOnUtc)
            VALUES (@EventId, UTC_TIMESTAMP(6))
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            commandText: insertInboxSql,
            parameters: new { EventId = eventId },
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static Task<int> ApplyRefundAsync(
        DbConnection connection,
        DbTransaction transaction,
        OrderRefundedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        const string applyRefundSql = """
            UPDATE sales_facts
            SET    refund_total = refund_total + @RefundAmount,
                   net_revenue  = net_revenue  - @RefundAmount
            WHERE  order_id = @OrderId
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            commandText: applyRefundSql,
            parameters: new { integrationEvent.OrderId, integrationEvent.RefundAmount },
            transaction: transaction,
            cancellationToken: cancellationToken));
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

    private static Task<decimal?> ReadCustomerLifetimeValueAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        const string readLifetimeValueSql =
            "SELECT lifetime_value FROM customer_lifetime_value WHERE customer_id = @CustomerId";

        return connection.QuerySingleOrDefaultAsync<decimal?>(new CommandDefinition(
            commandText: readLifetimeValueSql,
            parameters: new { CustomerId = customerId },
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static Task<int> UpsertCustomerSegmentAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid customerId,
        string segment,
        DateTime segmentOnUtc,
        decimal lifetimeValue,
        CancellationToken cancellationToken)
    {
        const string upsertSegmentSql = """
            INSERT INTO customer_segment_snapshot (customer_id, segment, segment_on_utc, lifetime_value)
            VALUES (@CustomerId, @Segment, @SegmentOnUtc, @LifetimeValue)
            ON DUPLICATE KEY UPDATE
                segment        = VALUES(segment),
                segment_on_utc = VALUES(segment_on_utc),
                lifetime_value = VALUES(lifetime_value)
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            commandText: upsertSegmentSql,
            parameters: new { CustomerId = customerId, Segment = segment, SegmentOnUtc = segmentOnUtc, LifetimeValue = lifetimeValue },
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static Task<int> UpsertInventorySnapshotAsync(
        DbConnection connection,
        DbTransaction transaction,
        ProductCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        const string upsertInventorySnapshotSql = """
            INSERT INTO inventory_snapshot
                (product_sku, product_name, on_hand, reorder_threshold, overstock_threshold, unit_cost, updated_on_utc)
            VALUES
                (@ProductSku, @ProductName, @OnHand, @ReorderThreshold, @OverstockThreshold, @UnitCost, UTC_TIMESTAMP(6))
            ON DUPLICATE KEY UPDATE
                product_name   = VALUES(product_name),
                on_hand        = VALUES(on_hand),
                unit_cost      = VALUES(unit_cost),
                updated_on_utc = VALUES(updated_on_utc)
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            commandText: upsertInventorySnapshotSql,
            parameters: new
            {
                integrationEvent.ProductSku,
                integrationEvent.ProductName,
                OnHand = integrationEvent.InitialStockQuantity,
                ReorderThreshold = DefaultReorderThreshold,
                OverstockThreshold = DefaultOverstockThreshold,
                UnitCost = integrationEvent.BasePrice,
            },
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static Task<int> InsertScheduledDeliveryFactAsync(
        DbConnection connection,
        DbTransaction transaction,
        DeliveryScheduledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        const string insertScheduledDeliverySql = """
            INSERT INTO delivery_facts
                (delivery_id, order_id, customer_id, scheduled_start_utc, scheduled_end_utc,
                 completed_on_utc, outcome, duration_minutes)
            VALUES
                (@DeliveryId, @OrderId, @CustomerId, @ScheduledStartUtc, @ScheduledEndUtc, NULL, NULL, NULL)
            ON DUPLICATE KEY UPDATE
                order_id            = VALUES(order_id),
                customer_id         = VALUES(customer_id),
                scheduled_start_utc = VALUES(scheduled_start_utc),
                scheduled_end_utc   = VALUES(scheduled_end_utc)
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            commandText: insertScheduledDeliverySql,
            parameters: new
            {
                integrationEvent.DeliveryId,
                integrationEvent.OrderId,
                integrationEvent.CustomerId,
                ScheduledStartUtc = integrationEvent.SlotStartUtc.UtcDateTime,
                ScheduledEndUtc = integrationEvent.SlotEndUtc.UtcDateTime,
            },
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task CompleteDeliveryFactAsync(
        DbConnection connection,
        DbTransaction transaction,
        DeliveryCompletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        // The outcome is derived in SQL against the row's scheduled slot: on time if it completed by the
        // slot end, otherwise late. Duration is whole minutes from the slot start to completion.
        const string completeDeliverySql = """
            UPDATE delivery_facts
            SET completed_on_utc = @CompletedOnUtc,
                outcome          = CASE WHEN @CompletedOnUtc <= scheduled_end_utc THEN 'on_time' ELSE 'late' END,
                duration_minutes = TIMESTAMPDIFF(MINUTE, scheduled_start_utc, @CompletedOnUtc)
            WHERE delivery_id = @DeliveryId
            """;

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            commandText: completeDeliverySql,
            parameters: new { integrationEvent.DeliveryId, CompletedOnUtc = integrationEvent.DeliveredOnUtc.UtcDateTime },
            transaction: transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (rowsAffected == 0)
        {
            // The DeliveryScheduled projection has not arrived yet (events can be reordered). Throwing rolls
            // back this transaction — including the inbox latch — so MassTransit redelivers until it has.
            throw new InvalidOperationException(
                $"Delivery {integrationEvent.DeliveryId} has no scheduled fact to complete yet; will retry on redelivery.");
        }
    }
}
