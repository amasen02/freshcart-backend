using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Kpis;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Sales.Queries;

public sealed class GetSalesOverviewQueryHandler(
    ISalesReadWarehouse salesWarehouse,
    TimeProvider timeProvider)
    : IQueryHandler<GetSalesOverviewQuery, SalesOverviewDto>
{
    public async Task<SalesOverviewDto> Handle(GetSalesOverviewQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var currentPeriod = query.Period.ToPeriod(timeProvider);
        var previousPeriod = new ReportingPeriod(
            currentPeriod.FromUtc - currentPeriod.Duration,
            currentPeriod.FromUtc);

        var currentSnapshot = await salesWarehouse
            .GetAggregateAsync(currentPeriod, cancellationToken)
            .ConfigureAwait(false);

        var previousSnapshot = await salesWarehouse
            .GetAggregateAsync(previousPeriod, cancellationToken)
            .ConfigureAwait(false);

        var tiles = BuildHeadlineTiles(currentSnapshot, previousSnapshot);

        return new SalesOverviewDto(
            CurrentPeriod: currentPeriod,
            PreviousPeriod: previousPeriod,
            Current: currentSnapshot,
            Previous: previousSnapshot,
            Tiles: tiles);
    }

    private static IReadOnlyList<KpiMetric> BuildHeadlineTiles(
        SalesSnapshot current,
        SalesSnapshot previous)
    {
        return
        [
            new KpiMetric(
                Code: KpiCodes.GrossMerchandiseValue,
                DisplayName: "Gross merchandise value",
                CurrentValue: current.GrossRevenue,
                PreviousValue: previous.GrossRevenue,
                Unit: KpiUnit.Currency,
                Description: "Total order value before refunds."),
            new KpiMetric(
                Code: KpiCodes.NetRevenue,
                DisplayName: "Net revenue",
                CurrentValue: current.NetRevenue,
                PreviousValue: previous.NetRevenue,
                Unit: KpiUnit.Currency,
                Description: "After refunds, discounts and tax adjustments."),
            new KpiMetric(
                Code: KpiCodes.OrderCount,
                DisplayName: "Orders",
                CurrentValue: current.OrderCount,
                PreviousValue: previous.OrderCount,
                Unit: KpiUnit.Count),
            new KpiMetric(
                Code: KpiCodes.AverageOrderValue,
                DisplayName: "Average order value",
                CurrentValue: current.AverageOrderValue,
                PreviousValue: previous.AverageOrderValue,
                Unit: KpiUnit.Currency),
            new KpiMetric(
                Code: KpiCodes.RefundRate,
                DisplayName: "Refund rate",
                CurrentValue: current.RefundRate * 100m,
                PreviousValue: previous.RefundRate * 100m,
                Unit: KpiUnit.Percentage,
                Description: "Refunded amount as a percentage of gross revenue."),
        ];
    }
}
