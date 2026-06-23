using FluentAssertions;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Features.Reviews.GetProductReviews;
using FreshCart.Reviews.Api.Persistence;

namespace FreshCart.Reviews.Tests.Reviews;

public sealed class RatingSummaryDtoTests
{
    [Fact]
    public void FoldsBucketsIntoCountAverageAndPerStarDistribution()
    {
        IReadOnlyList<RatingBucket> buckets =
        [
            new RatingBucket(5, 4),
            new RatingBucket(4, 3),
            new RatingBucket(1, 1),
        ];

        var summary = RatingSummaryDto.FromBuckets(buckets);

        summary.ReviewCount.Should().Be(8);
        // (5*4 + 4*3 + 1*1) / 8 = 33 / 8 = 4.125 -> 4.1 at one decimal.
        summary.AverageRating.Should().Be(4.1m);
        summary.CountsByStar[5].Should().Be(4);
        summary.CountsByStar[4].Should().Be(3);
        summary.CountsByStar[3].Should().Be(0);
        summary.CountsByStar[2].Should().Be(0);
        summary.CountsByStar[1].Should().Be(1);
    }

    [Fact]
    public void AlwaysCarriesEveryStarFromOneToFiveSoTheDistributionBarIsComplete()
    {
        IReadOnlyList<RatingBucket> buckets = [new RatingBucket(3, 2)];

        var summary = RatingSummaryDto.FromBuckets(buckets);

        summary.CountsByStar.Keys.Should().BeEquivalentTo(
            Enumerable.Range(ReviewConstraints.MinimumRating, ReviewConstraints.MaximumRating));
    }

    [Fact]
    public void ReportsAZeroAverageWithoutDividingByZeroWhenThereAreNoApprovedReviews()
    {
        var summary = RatingSummaryDto.FromBuckets([]);

        summary.ReviewCount.Should().Be(0);
        summary.AverageRating.Should().Be(0m);
        summary.CountsByStar.Values.Should().AllSatisfy(count => count.Should().Be(0));
    }

    [Fact]
    public void RoundsHalfwayAveragesToEvenAtOneDecimal()
    {
        // Two fives and one four: (5+5+4)/3 = 4.6666..., which rounds to 4.7.
        IReadOnlyList<RatingBucket> buckets = [new RatingBucket(5, 2), new RatingBucket(4, 1)];

        var summary = RatingSummaryDto.FromBuckets(buckets);

        summary.AverageRating.Should().Be(4.7m);
    }
}
