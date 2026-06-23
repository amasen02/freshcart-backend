using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Models;

namespace FreshCart.Reporting.Application.Sales.Queries;

/// <summary>
/// Composite query that fetches the two most common pie-chart breakdowns in one round-trip.
/// </summary>
public sealed record GetRevenueBreakdownQuery(PeriodSelector Period) : IQuery<RevenueBreakdownDto>;
