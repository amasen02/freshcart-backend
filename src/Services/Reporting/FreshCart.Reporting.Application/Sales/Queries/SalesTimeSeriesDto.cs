using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Sales.Queries;

/// <summary>
/// Bucketed sales points for the resolved period.
/// </summary>
public sealed record SalesTimeSeriesDto(
    ReportingPeriod Period,
    AggregationBucket Bucket,
    IReadOnlyList<SalesSnapshot> Points);
