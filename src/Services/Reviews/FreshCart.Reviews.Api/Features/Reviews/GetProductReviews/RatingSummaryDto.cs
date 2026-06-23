using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Persistence;

namespace FreshCart.Reviews.Api.Features.Reviews.GetProductReviews;

/// <summary>
/// Aggregate rating picture for a product, folded from the per-star buckets the Mongo group pipeline
/// returns. The average is rounded to one decimal at this presentation boundary; the per-star counts
/// always carry every star from 1 to 5 so the storefront can render a full distribution bar without
/// guessing which keys are missing.
/// </summary>
public sealed record RatingSummaryDto(
    decimal AverageRating,
    long ReviewCount,
    IReadOnlyDictionary<int, long> CountsByStar)
{
    private const int RatingRoundingDecimals = 1;

    public static RatingSummaryDto FromBuckets(IReadOnlyList<RatingBucket> ratingBuckets)
    {
        ArgumentNullException.ThrowIfNull(ratingBuckets);

        var countsByStar = new Dictionary<int, long>(ReviewConstraints.MaximumRating);
        for (var star = ReviewConstraints.MinimumRating; star <= ReviewConstraints.MaximumRating; star++)
        {
            countsByStar[star] = 0;
        }

        long reviewCount = 0;
        long weightedSum = 0;
        foreach (var bucket in ratingBuckets)
        {
            countsByStar[bucket.Rating] = bucket.Count;
            reviewCount += bucket.Count;
            weightedSum += bucket.Rating * bucket.Count;
        }

        var averageRating = reviewCount == 0
            ? 0m
            : Math.Round((decimal)weightedSum / reviewCount, RatingRoundingDecimals, MidpointRounding.ToEven);

        return new RatingSummaryDto(averageRating, reviewCount, countsByStar);
    }
}
