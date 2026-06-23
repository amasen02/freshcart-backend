using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Catalog.Api.Models;
using Marten;
using Marten.Pagination;

namespace FreshCart.Catalog.Api.Features.Products.SearchProducts;

/// <summary>
/// Full-text product search over the Postgres tsvector index Marten maintains on name and
/// description. Web-style parsing lets shoppers type natural phrases including quoted terms.
/// </summary>
public sealed class SearchProductsQueryHandler(IQuerySession querySession)
    : IQueryHandler<SearchProductsQuery, PaginatedResult<ProductSummaryDto>>
{
    public async Task<PaginatedResult<ProductSummaryDto>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pagination = query.Pagination.Normalise();

        var matchesPage = await querySession.Query<Product>()
            .Where(product => product.IsActive)
            .Where(product => product.WebStyleSearch(query.Term))
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Sku)
            .ToPagedListAsync(pagination.PageNumber, pagination.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return new PaginatedResult<ProductSummaryDto>(
            pagination.PageNumber,
            pagination.PageSize,
            matchesPage.TotalItemCount,
            matchesPage.Select(ProductSummaryDto.FromProduct).ToList());
    }
}
