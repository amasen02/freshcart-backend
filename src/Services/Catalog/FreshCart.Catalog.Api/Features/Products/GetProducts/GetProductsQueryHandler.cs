using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Catalog.Api.Models;
using Marten;
using Marten.Pagination;

namespace FreshCart.Catalog.Api.Features.Products.GetProducts;

public sealed class GetProductsQueryHandler(IQuerySession querySession)
    : IQueryHandler<GetProductsQuery, PaginatedResult<ProductSummaryDto>>
{
    public async Task<PaginatedResult<ProductSummaryDto>> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pagination = query.Pagination.Normalise();

        var filteredProducts = ProductQueryFilters.ApplyFilters(
            querySession.Query<Product>(),
            query.CategoryId,
            query.BrandId,
            query.MaxPrice,
            query.IsDigital,
            query.IncludeInactive);

        var sortedProducts = ProductQueryFilters.ApplySort(filteredProducts, query.SortOption);

        var productsPage = await sortedProducts
            .ToPagedListAsync(pagination.PageNumber, pagination.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return new PaginatedResult<ProductSummaryDto>(
            pagination.PageNumber,
            pagination.PageSize,
            productsPage.TotalItemCount,
            productsPage.Select(ProductSummaryDto.FromProduct).ToList());
    }
}
