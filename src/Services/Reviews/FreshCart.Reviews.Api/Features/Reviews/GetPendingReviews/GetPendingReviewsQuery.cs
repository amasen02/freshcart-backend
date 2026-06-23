using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;

namespace FreshCart.Reviews.Api.Features.Reviews.GetPendingReviews;

public sealed record GetPendingReviewsQuery(PaginationRequest Pagination)
    : IQuery<PaginatedResult<ReviewDto>>;
