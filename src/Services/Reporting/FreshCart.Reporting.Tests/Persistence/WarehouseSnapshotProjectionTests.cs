using System.Globalization;
using Dapper;
using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Domain.Sales;
using FreshCart.Reporting.Infrastructure.Persistence.Warehouse;
using MySqlConnector;

namespace FreshCart.Reporting.Tests.Persistence;

/// <summary>
/// Proves the REP-SCHEMA work against a real MySQL warehouse: the production schema provisions every
/// analytical table, and the three previously-unwritten snapshot tables now have correct, idempotent
/// projection writers feeding the dashboards (inventory health, delivery performance, customer
/// acquisition). Each time-windowed assertion uses a far-future window unique to the test so the shared
/// container's accumulated rows cannot perturb it.
/// </summary>
[Collection(WarehouseIntegrationCollection.Name)]
public sealed class WarehouseSnapshotProjectionTests(WarehouseIntegrationFixture fixture)
{
    private readonly WarehouseProjectionWriter writer = new(fixture.ConnectionFactory);
    private readonly DapperProductReadWarehouse productReadWarehouse = new(fixture.ConnectionFactory);
    private readonly DapperDeliveryReadWarehouse deliveryReadWarehouse = new(fixture.ConnectionFactory);
    private readonly DapperCustomerReadWarehouse customerReadWarehouse = new(fixture.ConnectionFactory);

    [Fact]
    public async Task ReportingWarehouseSchemaProvisionsEveryAnalyticalTable()
    {
        var connection = new MySqlConnection(fixture.ConnectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync();
            await ReportingWarehouseSchema.EnsureCreatedAsync(connection, CancellationToken.None);

            var tables = (await connection.QueryAsync<string>(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = DATABASE()"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            tables.Should().Contain([
                "sales_facts", "sales_line_facts", "customer_lifetime_value",
                "customer_segment_snapshot", "inventory_snapshot", "delivery_facts",
            ]);
        }
    }

    [Fact]
    public async Task ProductCreatedSeedsAnInventorySnapshotExactlyOnce()
    {
        var productSku = "SKU-INV-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var productCreated = new ProductCreatedIntegrationEvent
        {
            ProductId = Guid.NewGuid(),
            ProductSku = productSku,
            ProductName = "Test Widget",
            PrimaryCategory = "Widgets",
            BasePrice = 12.50m,
            CurrencyCode = "USD",
            InitialStockQuantity = 40,
            IsDigital = false,
        };

        var firstApply = await writer.ApplyProductCreatedAsync(productCreated, CancellationToken.None);
        var redelivery = await writer.ApplyProductCreatedAsync(productCreated, CancellationToken.None);

        firstApply.Should().BeTrue();
        redelivery.Should().BeFalse();

        var snapshot = await ReadInventorySnapshotAsync(productSku);
        snapshot.OnHand.Should().Be(40);
        snapshot.UnitCost.Should().Be(12.50m);
        snapshot.ReorderThreshold.Should().Be(10);
        snapshot.OverstockThreshold.Should().Be(1000);

        var health = await productReadWarehouse.GetInventoryHealthAsync(CancellationToken.None);
        health.TotalSkus.Should().BeGreaterThanOrEqualTo(1);
        health.InventoryValueAtCost.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task ScheduledThenCompletedDeliveryRecordsAnOnTimeFact()
    {
        var deliveryId = Guid.NewGuid();
        var slotStart = new DateTimeOffset(2099, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var slotEnd = slotStart.AddHours(3);
        await ScheduleDeliveryAsync(deliveryId, slotStart, slotEnd);

        await writer.ApplyDeliveryCompletedAsync(
            CompletedEvent(deliveryId, completedOnUtc: slotStart.AddHours(2)), CancellationToken.None);

        var summary = await deliveryReadWarehouse.GetPerformanceSummaryAsync(
            new ReportingPeriod(slotStart, slotStart.AddDays(1)), CancellationToken.None);

        summary.TotalDeliveries.Should().Be(1);
        summary.OnTimeCount.Should().Be(1);
        summary.LateCount.Should().Be(0);
        summary.OnTimePercentage.Should().Be(100m);
        summary.AverageDurationMinutes.Should().Be(120m);
    }

    [Fact]
    public async Task ACompletionAfterTheSlotEndIsRecordedAsLate()
    {
        var deliveryId = Guid.NewGuid();
        var slotStart = new DateTimeOffset(2099, 2, 1, 9, 0, 0, TimeSpan.Zero);
        var slotEnd = slotStart.AddHours(3);
        await ScheduleDeliveryAsync(deliveryId, slotStart, slotEnd);

        await writer.ApplyDeliveryCompletedAsync(
            CompletedEvent(deliveryId, completedOnUtc: slotEnd.AddHours(1)), CancellationToken.None);

        var summary = await deliveryReadWarehouse.GetPerformanceSummaryAsync(
            new ReportingPeriod(slotStart, slotStart.AddDays(1)), CancellationToken.None);

        summary.TotalDeliveries.Should().Be(1);
        summary.LateCount.Should().Be(1);
        summary.OnTimeCount.Should().Be(0);
        summary.OnTimePercentage.Should().Be(0m);
    }

    [Fact]
    public Task CompletingADeliveryBeforeItsScheduledFactExistsThrowsSoMassTransitRetries()
    {
        var completeMissingDelivery = () => writer.ApplyDeliveryCompletedAsync(
            CompletedEvent(Guid.NewGuid(), completedOnUtc: new DateTimeOffset(2099, 3, 1, 12, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        return completeMissingDelivery.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task FirstOrderSegmentsTheCustomerAsNewAndASecondAsReturning()
    {
        var customerId = Guid.NewGuid();
        var firstOrderAt = new DateTimeOffset(2099, 4, 1, 10, 0, 0, TimeSpan.Zero);
        await writer.ApplyOrderConfirmedAsync(
            ConfirmedOrder(customerId, firstOrderAt, grandTotal: 50m), CancellationToken.None);

        var afterFirstOrder = await customerReadWarehouse.GetAcquisitionSummaryAsync(
            new ReportingPeriod(firstOrderAt.AddHours(-1), firstOrderAt.AddHours(1)), CancellationToken.None);
        afterFirstOrder.NewCustomers.Should().Be(1);
        afterFirstOrder.ReturningCustomers.Should().Be(0);

        var secondOrderAt = new DateTimeOffset(2099, 4, 2, 10, 0, 0, TimeSpan.Zero);
        await writer.ApplyOrderConfirmedAsync(
            ConfirmedOrder(customerId, secondOrderAt, grandTotal: 30m), CancellationToken.None);

        var afterSecondOrder = await customerReadWarehouse.GetAcquisitionSummaryAsync(
            new ReportingPeriod(secondOrderAt.AddHours(-1), secondOrderAt.AddHours(1)), CancellationToken.None);
        afterSecondOrder.ReturningCustomers.Should().Be(1);
        afterSecondOrder.NewCustomers.Should().Be(0);
        afterSecondOrder.AverageLifetimeValue.Should().Be(80m);
    }

    private async Task ScheduleDeliveryAsync(Guid deliveryId, DateTimeOffset slotStart, DateTimeOffset slotEnd)
    {
        var scheduled = new DeliveryScheduledIntegrationEvent
        {
            OrderId = Guid.NewGuid(),
            DeliveryId = deliveryId,
            CustomerId = Guid.NewGuid(),
            SlotStartUtc = slotStart,
            SlotEndUtc = slotEnd,
        };

        var applied = await writer.ApplyDeliveryScheduledAsync(scheduled, CancellationToken.None);
        applied.Should().BeTrue();
    }

    private static DeliveryCompletedIntegrationEvent CompletedEvent(Guid deliveryId, DateTimeOffset completedOnUtc) => new()
    {
        OrderId = Guid.NewGuid(),
        DeliveryId = deliveryId,
        CustomerId = Guid.NewGuid(),
        DeliveredOnUtc = completedOnUtc,
    };

    private static OrderConfirmedIntegrationEvent ConfirmedOrder(Guid customerId, DateTimeOffset occurredOnUtc, decimal grandTotal) => new()
    {
        OrderId = Guid.NewGuid(),
        CustomerId = customerId,
        OccurredOnUtc = occurredOnUtc,
        GrandTotal = grandTotal,
        DiscountTotal = 0m,
        TaxTotal = 0m,
        ShippingTotal = 0m,
        CurrencyCode = "USD",
        PaymentMethod = "Card",
        Lines = [new OrderConfirmedLine("SKU-SEG", "Segment Probe", "Probe", 1, grandTotal)],
    };

    private async Task<InventorySnapshotRow> ReadInventorySnapshotAsync(string productSku)
    {
        var connection = new MySqlConnection(fixture.ConnectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync();
            return await connection.QuerySingleAsync<InventorySnapshotRow>(
                """
                SELECT on_hand AS OnHand, unit_cost AS UnitCost,
                       reorder_threshold AS ReorderThreshold, overstock_threshold AS OverstockThreshold
                FROM inventory_snapshot WHERE product_sku = @ProductSku
                """,
                new { ProductSku = productSku });
        }
    }

    private sealed record InventorySnapshotRow(int OnHand, decimal UnitCost, int ReorderThreshold, int OverstockThreshold);
}
