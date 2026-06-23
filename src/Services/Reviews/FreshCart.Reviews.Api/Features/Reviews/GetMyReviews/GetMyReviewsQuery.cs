using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;

namespace FreshCart.Reviews.Api.Features.Reviews.GetMyReviews;

public sealed record GetMyReviewsQuery(Guid CustomerId, PaginationRequest Pagination)
    : IQuery<PaginatedResult<ReviewDto>>;
