using FluentAssertions;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Application.Common.Models;
using FreshCart.Reporting.Application.Sales.Queries;
using FreshCart.Reporting.Domain.Kpis;
using FreshCart.Reporting.Domain.Sales;
using NSubstitute;

namespace FreshCart.Reporting.Tests.Dashboards;

/// <summary>
/// Unit tests for the headline-KPI handler. The warehouse is substituted so the test exercises
/// only the period-derivation + tile-building behaviour of the handler.
/// </summary>
public sealed class GetSalesOverviewQueryHandlerTests
{
    private static readonly DateTimeOffset FixedNowUtc = new(2026, 5, 25, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReturnsHeadlineTilesWithCurrentAndPreviousPeriodValues()
    {
        var fixedClock = new FakeTimeProvider(FixedNowUtc);
        var salesWarehouse = Substitute.For<ISalesReadWarehouse>();

        // The Last30Days preset produces a current window ending exactly at the fixed clock,
        // so any other window passed to the warehouse must be the derived previous period.
        salesWarehouse
            .GetAggregateAsync(Arg.Any<ReportingPeriod>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<ReportingPeriod>().ToUtcExclusive == FixedNowUtc
                ? MakeSnapshot(grossRevenue: 2_400m, orderCount: 120, refundTotal: 18m, netRevenue: 1_980m)
                : MakeSnapshot(grossRevenue: 2_000m, orderCount: 100, refundTotal: 12m, netRevenue: 1_700m));

        var handler = new GetSalesOverviewQueryHandler(salesWarehouse, fixedClock);
        var query = new GetSalesOverviewQuery(new PeriodSelector(PeriodPreset.Last30Days));

        var dto = await handler.Handle(query, CancellationToken.None);

        dto.Current.GrossRevenue.Should().Be(2_400m);
        dto.Previous.GrossRevenue.Should().Be(2_000m);
        dto.PreviousPeriod.ToUtcExclusive.Should().Be(dto.CurrentPeriod.FromUtc);

        var gmvTile = dto.Tiles.Single(tile => string.Equals(tile.Code, KpiCodes.GrossMerchandiseValue, StringComparison.Ordinal));
        gmvTile.CurrentValue.Should().Be(2_400m);
        gmvTile.PreviousValue.Should().Be(2_000m);
        gmvTile.DeltaPercentage.Should().Be(20m);
        gmvTile.Trend.Should().Be(KpiTrend.Up);

        var aovTile = dto.Tiles.Single(tile => string.Equals(tile.Code, KpiCodes.AverageOrderValue, StringComparison.Ordinal));
        aovTile.CurrentValue.Should().Be(20m);
        aovTile.PreviousValue.Should().Be(20m);
        aovTile.Trend.Should().Be(KpiTrend.Flat);
    }

    private static SalesSnapshot MakeSnapshot(decimal grossRevenue, int orderCount, decimal refundTotal, decimal netRevenue)
        => new(
            Day: DateOnly.FromDateTime(FixedNowUtc.UtcDateTime),
            OrderCount: orderCount,
            UniqueCustomerCount: orderCount - 5,
            GrossRevenue: grossRevenue,
            DiscountTotal: 0,
            RefundTotal: refundTotal,
            TaxTotal: 0,
            ShippingTotal: 0,
            NetRevenue: netRevenue);

    private sealed class FakeTimeProvider(DateTimeOffset fixedNowUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => fixedNowUtc;
    }
}
