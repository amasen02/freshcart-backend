using System.Data.Common;
using Dapper;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// Idempotent DDL for the warehouse's Dapper-accessed analytical tables — the fact tables the projection
/// consumers write and the snapshot tables the dashboards read. It owns every table that EF Core does not
/// (the invoice tables and the projection inbox are created by <c>WarehouseDbContext.EnsureCreatedAsync</c>);
/// keeping the script here as <c>CREATE TABLE IF NOT EXISTS</c> lets the startup initializer and the
/// integration tests provision the exact same schema, and makes re-running on every boot a no-op.
/// </summary>
public static class ReportingWarehouseSchema
{
    private const string CreateTablesSql = """
        CREATE TABLE IF NOT EXISTS sales_facts (
            order_id        CHAR(36)       NOT NULL PRIMARY KEY,
            customer_id     CHAR(36)       NOT NULL,
            occurred_on_utc DATETIME(6)    NOT NULL,
            gross_revenue   DECIMAL(18,2)  NOT NULL,
            discount_total  DECIMAL(18,2)  NOT NULL,
            refund_total    DECIMAL(18,2)  NOT NULL,
            tax_total       DECIMAL(18,2)  NOT NULL,
            shipping_total  DECIMAL(18,2)  NOT NULL,
            net_revenue     DECIMAL(18,2)  NOT NULL,
            payment_method  VARCHAR(64)    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS sales_line_facts (
            order_id         CHAR(36)      NOT NULL,
            product_sku      VARCHAR(64)   NOT NULL,
            product_name     VARCHAR(256)  NOT NULL,
            primary_category VARCHAR(128)  NOT NULL,
            quantity         INT           NOT NULL,
            unit_price       DECIMAL(18,2) NOT NULL,
            net_revenue      DECIMAL(18,2) NOT NULL,
            occurred_on_utc  DATETIME(6)   NOT NULL,
            PRIMARY KEY (order_id, product_sku)
        );

        CREATE TABLE IF NOT EXISTS customer_lifetime_value (
            customer_id    CHAR(36)      NOT NULL PRIMARY KEY,
            display_name   VARCHAR(128)  NOT NULL,
            order_count    INT           NOT NULL,
            lifetime_value DECIMAL(18,2) NOT NULL
        );

        CREATE TABLE IF NOT EXISTS customer_segment_snapshot (
            customer_id    CHAR(36)      NOT NULL PRIMARY KEY,
            segment        VARCHAR(32)   NOT NULL,
            segment_on_utc DATETIME(6)   NOT NULL,
            lifetime_value DECIMAL(18,2) NOT NULL
        );

        CREATE TABLE IF NOT EXISTS inventory_snapshot (
            product_sku         VARCHAR(64)   NOT NULL PRIMARY KEY,
            product_name        VARCHAR(256)  NOT NULL,
            on_hand             INT           NOT NULL,
            reorder_threshold   INT           NOT NULL,
            overstock_threshold INT           NOT NULL,
            unit_cost           DECIMAL(18,2) NOT NULL,
            updated_on_utc      DATETIME(6)   NOT NULL
        );

        CREATE TABLE IF NOT EXISTS delivery_facts (
            delivery_id         CHAR(36)    NOT NULL PRIMARY KEY,
            order_id            CHAR(36)    NOT NULL,
            customer_id         CHAR(36)    NOT NULL,
            scheduled_start_utc DATETIME(6) NOT NULL,
            scheduled_end_utc   DATETIME(6) NOT NULL,
            completed_on_utc    DATETIME(6) NULL,
            outcome             VARCHAR(32) NULL,
            duration_minutes    INT         NULL
        );
        """;

    public static Task EnsureCreatedAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection.ExecuteAsync(new CommandDefinition(CreateTablesSql, cancellationToken: cancellationToken));
    }
}
