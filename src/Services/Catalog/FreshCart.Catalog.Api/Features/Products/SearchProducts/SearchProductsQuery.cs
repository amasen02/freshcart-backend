using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;

namespace FreshCart.Catalog.Api.Features.Products.SearchProducts;

public sealed record SearchProductsQuery(string Term, PaginationRequest Pagination)
    : IQuery<PaginatedResult<ProductSummaryDto>>;
