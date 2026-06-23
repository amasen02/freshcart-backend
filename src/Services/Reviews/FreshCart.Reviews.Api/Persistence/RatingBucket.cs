namespace FreshCart.Reviews.Api.Persistence;

/// <summary>
/// One row of the rating aggregation: how many approved reviews awarded a given star value. The handler
/// folds these buckets into the public <c>RatingSummaryDto</c>.
/// </summary>
public sealed record RatingBucket(int Rating, long Count);
