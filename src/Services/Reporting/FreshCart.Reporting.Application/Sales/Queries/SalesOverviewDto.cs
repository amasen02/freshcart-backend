using FreshCart.Reporting.Domain.Kpis;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Sales.Queries;

/// <summary>
/// Headline KPI tiles plus the raw snapshots for the current and previous periods.
/// </summary>
public sealed record SalesOverviewDto(
    ReportingPeriod CurrentPeriod,
    ReportingPeriod PreviousPeriod,
    SalesSnapshot Current,
    SalesSnapshot Previous,
    IReadOnlyList<KpiMetric> Tiles);
