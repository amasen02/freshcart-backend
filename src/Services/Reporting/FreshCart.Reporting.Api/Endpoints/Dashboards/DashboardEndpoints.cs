using Carter;
using FreshCart.Reporting.Api.Authentication;
using FreshCart.Reporting.Application.Common.Models;
using FreshCart.Reporting.Application.Delivery.Queries;
using FreshCart.Reporting.Application.Inventory.Queries;
using FreshCart.Reporting.Application.Sales.Queries;
using FreshCart.Reporting.Domain.Sales;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FreshCart.Reporting.Api.Endpoints.Dashboards;

/// <summary>
/// Carter module exposing the dashboard read endpoints. Every route is read-only and protected
/// by the <c>BackOfficeUser</c> policy (Administrator + SupportAgent + Manager roles).
/// </summary>
public sealed class DashboardEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var dashboardGroup = app
            .MapGroup("/dashboards")
            .RequireAuthorization(AuthorizationPolicies.BackOfficeUser)
            .WithTags("Dashboards");

        dashboardGroup.MapGet("/sales/overview", GetSalesOverviewAsync)
            .WithSummary("Headline sales KPIs (GMV, AOV, refund rate, orders) for the current and previous period.")
            .Produces<SalesOverviewDto>(StatusCodes.Status200OK);

        dashboardGroup.MapGet("/sales/time-series", GetSalesTimeSeriesAsync)
            .WithSummary("Per-bucket sales totals for the trend chart.")
            .Produces<SalesTimeSeriesDto>(StatusCodes.Status200OK);

        dashboardGroup.MapGet("/sales/breakdown", GetRevenueBreakdownAsync)
            .WithSummary("Revenue split by category and by payment method.")
            .Produces<RevenueBreakdownDto>(StatusCodes.Status200OK);

        dashboardGroup.MapGet("/inventory/health", GetInventoryHealthAsync)
            .WithSummary("Stock-on-hand health: out-of-stock, low-stock and overstock counts.")
            .Produces(StatusCodes.Status200OK);

        dashboardGroup.MapGet("/delivery/performance", GetDeliveryPerformanceAsync)
            .WithSummary("Delivery success / on-time / late / failed counts and averages for the period.")
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetSalesOverviewAsync(
        ISender mediator,
        PeriodPreset? preset,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var query = new GetSalesOverviewQuery(new PeriodSelector(preset, fromUtc, toUtc));
        return Results.Ok(await mediator.Send(query, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<IResult> GetSalesTimeSeriesAsync(
        ISender mediator,
        PeriodPreset? preset,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        AggregationBucket bucket,
        CancellationToken cancellationToken)
    {
        var query = new GetSalesTimeSeriesQuery(new PeriodSelector(preset, fromUtc, toUtc), bucket);
        return Results.Ok(await mediator.Send(query, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<IResult> GetRevenueBreakdownAsync(
        ISender mediator,
        PeriodPreset? preset,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var query = new GetRevenueBreakdownQuery(new PeriodSelector(preset, fromUtc, toUtc));
        return Results.Ok(await mediator.Send(query, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<IResult> GetInventoryHealthAsync(ISender mediator, CancellationToken cancellationToken)
        => Results.Ok(await mediator.Send(new GetInventoryHealthQuery(), cancellationToken).ConfigureAwait(false));

    private static async Task<IResult> GetDeliveryPerformanceAsync(
        ISender mediator,
        PeriodPreset? preset,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var query = new GetDeliveryPerformanceQuery(new PeriodSelector(preset, fromUtc, toUtc));
        return Results.Ok(await mediator.Send(query, cancellationToken).ConfigureAwait(false));
    }
}
