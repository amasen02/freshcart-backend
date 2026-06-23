using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Reviews.Api.Persistence;

namespace FreshCart.Reviews.Api.Features.Reviews.GetProductReviews;

/// <summary>
/// Storefront read: the approved-reviews page and the aggregate rating summary for the same product. The
/// summary is computed by a Mongo group aggregation here in the handler rather than by counting the page
/// in memory, so the average and per-star distribution reflect every approved review, not just the page.
/// </summary>
public sealed class GetProductReviewsQueryHandler(IReviewRepository reviewRepository)
    : IQueryHandler<GetProductReviewsQuery, ProductReviewsResponse>
{
    public async Task<ProductReviewsResponse> Handle(GetProductReviewsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var ratingBuckets = await reviewRepository
            .GetApprovedRatingBucketsForProductAsync(query.ProductSku, cancellationToken)
            .ConfigureAwait(false);

        var approvedReviewsPage = await reviewRepository
            .GetApprovedForProductAsync(query.ProductSku, query.Pagination, cancellationToken)
            .ConfigureAwait(false);

        var summary = RatingSummaryDto.FromBuckets(ratingBuckets);

        var reviews = new PaginatedResult<ReviewDto>(
            approvedReviewsPage.PageNumber,
            approvedReviewsPage.PageSize,
            approvedReviewsPage.TotalItemCount,
            approvedReviewsPage.Items.Select(ReviewDto.FromReview).ToList());

        return new ProductReviewsResponse(summary, reviews);
    }
}
