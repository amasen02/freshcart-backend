using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Persistence;

namespace FreshCart.Reviews.Api.Features.Reviews.CreateReview;

/// <summary>
/// Records a new review in the moderation-first <see cref="ReviewStatus.Pending"/> state. The verified
/// -purchase badge is decided from the locally held purchase entitlements, not a runtime call to
/// Ordering, and the one-review-per-product-per-customer rule is checked before the write so a duplicate
/// surfaces as a 409 rather than a raw index violation.
/// </summary>
public sealed class CreateReviewCommandHandler(
    IReviewRepository reviewRepository,
    IPurchaseRecordRepository purchaseRecordRepository,
    TimeProvider timeProvider)
    : ICommandHandler<CreateReviewCommand, CreateReviewResult>
{
    public async Task<CreateReviewResult> Handle(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await reviewRepository
                .ExistsForCustomerAsync(command.ProductSku, command.CustomerId, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new ConflictException($"You have already reviewed product \"{command.ProductSku}\".");
        }

        var isVerifiedPurchase = await purchaseRecordRepository
            .HasPurchasedAsync(command.CustomerId, command.ProductSku, cancellationToken)
            .ConfigureAwait(false);

        var review = ProductReview.Submit(
            Guid.CreateVersion7(),
            command.ProductSku,
            command.CustomerId,
            command.CustomerDisplayName,
            command.Rating,
            command.Title.Trim(),
            command.Body.Trim(),
            isVerifiedPurchase,
            timeProvider.GetUtcNow());

        await reviewRepository.InsertAsync(review, cancellationToken).ConfigureAwait(false);

        return new CreateReviewResult(review.Id, review.Status, review.IsVerifiedPurchase);
    }
}
