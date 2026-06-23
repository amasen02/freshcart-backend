using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Models;

namespace FreshCart.Reporting.Application.Products.Queries;

/// <summary>
/// Returns the ranked product list for the selected period, either best sellers or slow movers.
/// </summary>
public sealed record GetTopProductsQuery(
    PeriodSelector Period,
    int Take = 20,
    TopProductsMode Mode = TopProductsMode.BestSellers) : IQuery<TopProductsDto>;
