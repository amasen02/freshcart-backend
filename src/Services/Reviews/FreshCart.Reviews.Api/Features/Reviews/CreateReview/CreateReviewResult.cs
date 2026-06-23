using FreshCart.Reviews.Api.Domain;

namespace FreshCart.Reviews.Api.Features.Reviews.CreateReview;

public sealed record CreateReviewResult(Guid ReviewId, ReviewStatus Status, bool IsVerifiedPurchase);
