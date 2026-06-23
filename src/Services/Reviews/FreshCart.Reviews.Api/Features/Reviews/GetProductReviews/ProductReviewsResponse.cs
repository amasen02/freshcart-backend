using FreshCart.BuildingBlocks.Pagination;

namespace FreshCart.Reviews.Api.Features.Reviews.GetProductReviews;

/// <summary>
/// Storefront review payload: the approved reviews page plus the aggregate rating picture for the whole
/// product, so the page header can show the average and distribution without a second request.
/// </summary>
public sealed record ProductReviewsResponse(
    RatingSummaryDto Summary,
    PaginatedResult<ReviewDto> Reviews);
