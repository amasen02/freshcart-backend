using FreshCart.Reviews.Api.Domain;

namespace FreshCart.Reviews.Api.Features.Reviews.ModerateReview;

public sealed record ModerateReviewResult(Guid ReviewId, ReviewStatus Status, DateTimeOffset ModeratedOnUtc);
