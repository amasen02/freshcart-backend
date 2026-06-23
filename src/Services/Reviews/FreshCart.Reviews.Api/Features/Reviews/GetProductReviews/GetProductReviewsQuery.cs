using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;

namespace FreshCart.Reviews.Api.Features.Reviews.GetProductReviews;

public sealed record GetProductReviewsQuery(string ProductSku, PaginationRequest Pagination)
    : IQuery<ProductReviewsResponse>;
