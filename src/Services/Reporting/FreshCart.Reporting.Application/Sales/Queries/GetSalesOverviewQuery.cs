using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Models;

namespace FreshCart.Reporting.Application.Sales.Queries;

/// <summary>
/// Powers the executive sales overview tile-strip. Returns headline KPIs (GMV, AOV, refund rate,
/// order count) plus the previous period values so the dashboard can render deltas and trends.
/// </summary>
public sealed record GetSalesOverviewQuery(PeriodSelector Period) : IQuery<SalesOverviewDto>;
