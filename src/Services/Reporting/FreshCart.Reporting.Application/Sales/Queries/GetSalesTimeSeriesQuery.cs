using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Models;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Sales.Queries;

/// <summary>
/// Returns the time-series for the sales-trend chart on the dashboard.
/// </summary>
public sealed record GetSalesTimeSeriesQuery(
    PeriodSelector Period,
    AggregationBucket Bucket = AggregationBucket.Daily) : IQuery<SalesTimeSeriesDto>;
