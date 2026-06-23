using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;

namespace FreshCart.Catalog.Api.Features.Products.GetProducts;

public sealed record GetProductsQuery(
    Guid? CategoryId,
    Guid? BrandId,
    decimal? MaxPrice,
    bool? IsDigital,
    ProductSortOption SortOption,
    bool IncludeInactive,
    PaginationRequest Pagination) : IQuery<PaginatedResult<ProductSummaryDto>>;
