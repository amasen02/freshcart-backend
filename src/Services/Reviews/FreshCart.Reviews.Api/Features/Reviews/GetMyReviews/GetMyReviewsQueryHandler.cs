using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Reviews.Api.Persistence;

namespace FreshCart.Reviews.Api.Features.Reviews.GetMyReviews;

/// <summary>
/// Returns every review the authenticated customer has written, in every status, so they can see that a
/// pending or rejected review of theirs has not silently vanished. The query is scoped to the token's
/// customer id, never a body parameter (BOLA prevention).
/// </summary>
public sealed class GetMyReviewsQueryHandler(IReviewRepository reviewRepository)
    : IQueryHandler<GetMyReviewsQuery, PaginatedResult<ReviewDto>>
{
    public async Task<PaginatedResult<ReviewDto>> Handle(GetMyReviewsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var reviewsPage = await reviewRepository
            .GetForCustomerAsync(query.CustomerId, query.Pagination, cancellationToken)
            .ConfigureAwait(false);

        return new PaginatedResult<ReviewDto>(
            reviewsPage.PageNumber,
            reviewsPage.PageSize,
            reviewsPage.TotalItemCount,
            reviewsPage.Items.Select(ReviewDto.FromReview).ToList());
    }
}
