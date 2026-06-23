using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Persistence;

namespace FreshCart.Reviews.Api.Features.Reviews.ModerateReview;

/// <summary>
/// Applies a moderator's decision to a review and stamps who decided and when. Re-moderation is
/// deliberately allowed and idempotent in outcome: applying the same decision twice leaves the review in
/// the same terminal state, and a moderator may overturn a previous decision (last decision wins).
/// </summary>
public sealed class ModerateReviewCommandHandler(
    IReviewRepository reviewRepository,
    TimeProvider timeProvider)
    : ICommandHandler<ModerateReviewCommand, ModerateReviewResult>
{
    public async Task<ModerateReviewResult> Handle(ModerateReviewCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var review = await reviewRepository.GetByIdAsync(command.ReviewId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ProductReview), command.ReviewId);

        var moderatedOnUtc = timeProvider.GetUtcNow();
        review.ApplyModeration(command.Decision, command.ModeratorId, moderatedOnUtc);

        await reviewRepository.ReplaceAsync(review, cancellationToken).ConfigureAwait(false);

        return new ModerateReviewResult(review.Id, review.Status, moderatedOnUtc);
    }
}
