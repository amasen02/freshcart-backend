using System.Globalization;
using FluentAssertions;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Features.Reviews.GetProductReviews;
using FreshCart.Reviews.Api.Persistence;
using FreshCart.Reviews.Tests.TestInfrastructure;
using MongoDB.Driver;

namespace FreshCart.Reviews.Tests.Persistence;

[Collection(MongoFixture.CollectionName)]
public sealed class MongoReviewRepositoryTests : IDisposable
{
    private const string ProductSku = "FC-PRD-0001";
    private const string OtherProductSku = "FC-PRD-0002";
    private static readonly DateTimeOffset BaseInstant = new(2026, 6, 18, 8, 0, 0, TimeSpan.Zero);

    private readonly MongoClient _mongoClient;
    private readonly ReviewsMongoContext _context;
    private readonly MongoReviewRepository _reviewRepository;

    public MongoReviewRepositoryTests(MongoFixture mongoFixture)
    {
        ArgumentNullException.ThrowIfNull(mongoFixture);

        _mongoClient = new MongoClient(mongoFixture.ConnectionString);

        var isolatedDatabaseName = $"reviews_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";
        _context = new ReviewsMongoContext(_mongoClient.GetDatabase(isolatedDatabaseName));
        _reviewRepository = new MongoReviewRepository(_context);
    }

    public void Dispose() => _mongoClient.Dispose();

    [Fact]
    public async Task RatingAggregationOverAKnownApprovedFixtureProducesTheExactAverageAndPerStarCounts()
    {
        await SeedApprovedAsync(ProductSku, 5, 5, 5, 4, 2);
        // A rejected and a pending review of the same product must not influence the summary.
        await InsertAsync(ReviewWith(ProductSku, rating: 1, ReviewStatus.Rejected, minutesOffset: 10));
        await InsertAsync(ReviewWith(ProductSku, rating: 1, ReviewStatus.Pending, minutesOffset: 11));
        // A different product's approved reviews must not leak into this product's summary.
        await SeedApprovedAsync(OtherProductSku, 1, 1);

        var buckets = await _reviewRepository.GetApprovedRatingBucketsForProductAsync(ProductSku, CancellationToken.None);
        var summary = RatingSummaryDto.FromBuckets(buckets);

        summary.ReviewCount.Should().Be(5);
        // (5+5+5+4+2)/5 = 21/5 = 4.2.
        summary.AverageRating.Should().Be(4.2m);
        summary.CountsByStar[5].Should().Be(3);
        summary.CountsByStar[4].Should().Be(1);
        summary.CountsByStar[3].Should().Be(0);
        summary.CountsByStar[2].Should().Be(1);
        summary.CountsByStar[1].Should().Be(0);
    }

    [Fact]
    public async Task ApprovedListingReturnsOnlyApprovedReviewsNewestFirstAndPagesThem()
    {
        await InsertAsync(ReviewWith(ProductSku, rating: 5, ReviewStatus.Approved, minutesOffset: 0));
        await InsertAsync(ReviewWith(ProductSku, rating: 4, ReviewStatus.Approved, minutesOffset: 10));
        await InsertAsync(ReviewWith(ProductSku, rating: 3, ReviewStatus.Approved, minutesOffset: 20));
        await InsertAsync(ReviewWith(ProductSku, rating: 1, ReviewStatus.Pending, minutesOffset: 30));

        var firstPage = await _reviewRepository.GetApprovedForProductAsync(
            ProductSku, new PaginationRequest(1, 2), CancellationToken.None);
        var secondPage = await _reviewRepository.GetApprovedForProductAsync(
            ProductSku, new PaginationRequest(2, 2), CancellationToken.None);

        firstPage.TotalItemCount.Should().Be(3, "the pending review is excluded");
        firstPage.Items.Select(review => review.Rating).Should().Equal(3, 4);
        secondPage.Items.Select(review => review.Rating).Should().Equal(5);
    }

    [Fact]
    public async Task MyReviewsListingReturnsEveryStatusForTheCustomerScopedToThatCustomer()
    {
        var customerId = Guid.CreateVersion7();
        var otherCustomerId = Guid.CreateVersion7();
        await InsertAsync(ReviewWith(ProductSku, rating: 5, ReviewStatus.Approved, minutesOffset: 0, customerId));
        await InsertAsync(ReviewWith(OtherProductSku, rating: 2, ReviewStatus.Pending, minutesOffset: 5, customerId));
        await InsertAsync(ReviewWith(ProductSku, rating: 1, ReviewStatus.Rejected, minutesOffset: 7, otherCustomerId));

        var mine = await _reviewRepository.GetForCustomerAsync(
            customerId, new PaginationRequest(1, 20), CancellationToken.None);

        mine.TotalItemCount.Should().Be(2);
        mine.Items.Select(review => review.Status)
            .Should().BeEquivalentTo([ReviewStatus.Pending, ReviewStatus.Approved]);
    }

    [Fact]
    public async Task PendingListingReturnsOnlyPendingReviews()
    {
        await EnsureIndexesAsync();
        await InsertAsync(ReviewWith(ProductSku, rating: 5, ReviewStatus.Pending, minutesOffset: 0));
        await InsertAsync(ReviewWith(OtherProductSku, rating: 4, ReviewStatus.Approved, minutesOffset: 5));

        var pending = await _reviewRepository.GetByStatusAsync(
            ReviewStatus.Pending, new PaginationRequest(1, 20), CancellationToken.None);

        pending.TotalItemCount.Should().Be(1);
        pending.Items.Should().ContainSingle().Which.ProductSku.Should().Be(ProductSku);
    }

    [Fact]
    public async Task ExistsForCustomerIsTrueOnlyForThatCustomersReviewOfThatProduct()
    {
        var customerId = Guid.CreateVersion7();
        await InsertAsync(ReviewWith(ProductSku, rating: 5, ReviewStatus.Approved, minutesOffset: 0, customerId));

        (await _reviewRepository.ExistsForCustomerAsync(ProductSku, customerId, CancellationToken.None))
            .Should().BeTrue();
        (await _reviewRepository.ExistsForCustomerAsync(OtherProductSku, customerId, CancellationToken.None))
            .Should().BeFalse();
        (await _reviewRepository.ExistsForCustomerAsync(ProductSku, Guid.CreateVersion7(), CancellationToken.None))
            .Should().BeFalse();
    }

    [Fact]
    public async Task TheUniqueIndexRejectsASecondReviewOfTheSameProductByTheSameCustomer()
    {
        await EnsureIndexesAsync();
        var customerId = Guid.CreateVersion7();
        await _reviewRepository.InsertAsync(
            ReviewWith(ProductSku, rating: 5, ReviewStatus.Approved, minutesOffset: 0, customerId),
            CancellationToken.None);

        var insertingDuplicate = () => _reviewRepository.InsertAsync(
            ReviewWith(ProductSku, rating: 1, ReviewStatus.Pending, minutesOffset: 5, customerId),
            CancellationToken.None);

        await insertingDuplicate.Should().ThrowAsync<MongoWriteException>();
    }

    [Fact]
    public async Task ReplacingAModeratedReviewPersistsTheNewStatusAndModerationStamp()
    {
        var review = ReviewWith(ProductSku, rating: 5, ReviewStatus.Pending, minutesOffset: 0);
        await InsertAsync(review);

        var moderatorId = Guid.CreateVersion7();
        review.ApplyModeration(ModerationDecision.Approved, moderatorId, BaseInstant.AddHours(1));
        await _reviewRepository.ReplaceAsync(review, CancellationToken.None);

        var reloaded = await _reviewRepository.GetByIdAsync(review.Id, CancellationToken.None);
        reloaded!.Status.Should().Be(ReviewStatus.Approved);
        reloaded.ModeratedBy.Should().Be(moderatorId);
        reloaded.ModeratedOnUtc.Should().Be(BaseInstant.AddHours(1));
    }

    private Task SeedApprovedAsync(string productSku, params int[] ratings)
    {
        var reviews = ratings
            .Select((rating, index) => ReviewWith(productSku, rating, ReviewStatus.Approved, minutesOffset: index))
            .ToList();

        return _context.Reviews.InsertManyAsync(reviews, options: null, CancellationToken.None);
    }

    private Task InsertAsync(ProductReview review) =>
        _context.Reviews.InsertOneAsync(review, options: null, CancellationToken.None);

    private Task EnsureIndexesAsync() =>
        new ReviewsPersistenceInitializer(_context, Microsoft.Extensions.Logging.Abstractions.NullLogger<ReviewsPersistenceInitializer>.Instance)
            .StartAsync(CancellationToken.None);

    private static ProductReview ReviewWith(
        string productSku,
        int rating,
        ReviewStatus status,
        int minutesOffset,
        Guid? customerId = null)
    {
        var review = ProductReview.Submit(
            Guid.CreateVersion7(),
            productSku,
            customerId ?? Guid.CreateVersion7(),
            "Dana Customer",
            rating,
            "Solid value",
            "Held up well over a month of daily use.",
            isVerifiedPurchase: true,
            createdOnUtc: BaseInstant.AddMinutes(minutesOffset));

        if (status != ReviewStatus.Pending)
        {
            review.ApplyModeration(
                status == ReviewStatus.Approved ? ModerationDecision.Approved : ModerationDecision.Rejected,
                Guid.CreateVersion7(),
                BaseInstant.AddMinutes(minutesOffset).AddMinutes(1));
        }

        return review;
    }
}
