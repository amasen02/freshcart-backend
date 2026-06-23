using Carter;
using FreshCart.Reporting.Api.Authentication;
using FreshCart.Reporting.Application.Common.Models;
using FreshCart.Reporting.Application.Customers.Queries;
using FreshCart.Reporting.Application.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FreshCart.Reporting.Api.Endpoints.Catalog;

public sealed class CatalogReportsEndpoints : ICarterModule
{
    private const int DefaultRankingSize = 20;

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var reportsGroup = app
            .MapGroup("/reports")
            .RequireAuthorization(AuthorizationPolicies.BackOfficeUser)
            .WithTags("Reports");

        reportsGroup.MapGet("/products/top", GetTopProductsAsync)
            .WithSummary("Top selling or slow-moving products by net revenue for the period.")
            .Produces<TopProductsDto>(StatusCodes.Status200OK);

        reportsGroup.MapGet("/customers/leaderboard", GetCustomerLeaderboardAsync)
            .WithSummary("Top customers by lifetime value.")
            .Produces<CustomerLeaderboardDto>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetTopProductsAsync(
        ISender mediator,
        PeriodPreset? preset,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int take,
        TopProductsMode mode,
        CancellationToken cancellationToken)
    {
        var query = new GetTopProductsQuery(
            new PeriodSelector(preset, fromUtc, toUtc),
            Take: take <= 0 ? DefaultRankingSize : take,
            Mode: mode);

        return Results.Ok(await mediator.Send(query, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<IResult> GetCustomerLeaderboardAsync(
        ISender mediator,
        int take,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerLeaderboardQuery(Take: take <= 0 ? DefaultRankingSize : take);
        return Results.Ok(await mediator.Send(query, cancellationToken).ConfigureAwait(false));
    }
}
