using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Persistence;

namespace FreshCart.Reviews.Api.Features.Reviews.GetPendingReviews;

/// <summary>
/// The moderation queue: every review still awaiting a decision, oldest-waiting surfaced through the
/// (Status, CreatedOnUtc) index so a back-office moderator works a stable, index-backed list.
/// </summary>
public sealed class GetPendingReviewsQueryHandler(IReviewRepository reviewRepository)
    : IQueryHandler<GetPendingReviewsQuery, PaginatedResult<ReviewDto>>
{
    public async Task<PaginatedResult<ReviewDto>> Handle(GetPendingReviewsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pendingPage = await reviewRepository
            .GetByStatusAsync(ReviewStatus.Pending, query.Pagination, cancellationToken)
            .ConfigureAwait(false);

        return new PaginatedResult<ReviewDto>(
            pendingPage.PageNumber,
            pendingPage.PageSize,
            pendingPage.TotalItemCount,
            pendingPage.Items.Select(ReviewDto.FromReview).ToList());
    }
}
