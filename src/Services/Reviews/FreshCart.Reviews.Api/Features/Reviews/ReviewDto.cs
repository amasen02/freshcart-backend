using FreshCart.Reviews.Api.Domain;

namespace FreshCart.Reviews.Api.Features.Reviews;

/// <summary>
/// Public read shape of a review. Status and moderation fields are included because the my-reviews and
/// moderation-queue listings surface them; the approved-storefront listing simply only ever returns
/// approved rows.
/// </summary>
public sealed record ReviewDto(
    Guid Id,
    string ProductSku,
    Guid CustomerId,
    string CustomerDisplayName,
    int Rating,
    string Title,
    string Body,
    bool IsVerifiedPurchase,
    ReviewStatus Status,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModeratedOnUtc,
    Guid? ModeratedBy)
{
    public static ReviewDto FromReview(ProductReview review)
    {
        ArgumentNullException.ThrowIfNull(review);

        return new ReviewDto(
            review.Id,
            review.ProductSku,
            review.CustomerId,
            review.CustomerDisplayName,
            review.Rating,
            review.Title,
            review.Body,
            review.IsVerifiedPurchase,
            review.Status,
            review.CreatedOnUtc,
            review.ModeratedOnUtc,
            review.ModeratedBy);
    }
}
