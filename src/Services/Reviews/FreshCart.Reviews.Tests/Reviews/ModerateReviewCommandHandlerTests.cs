using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Features.Reviews.ModerateReview;
using FreshCart.Reviews.Api.Persistence;
using FreshCart.Reviews.Tests.TestInfrastructure;
using NSubstitute;

namespace FreshCart.Reviews.Tests.Reviews;

public sealed class ModerateReviewCommandHandlerTests
{
    private static readonly DateTimeOffset KnownInstantUtc = new(2026, 6, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CreatedOnUtc = new(2026, 6, 17, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid ReviewId = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    private static readonly Guid ModeratorId = Guid.Parse("e0000000-0000-0000-0000-000000000001");
    private static readonly Guid CustomerId = Guid.Parse("c0000000-0000-0000-0000-000000000001");

    private readonly IReviewRepository reviewRepository = Substitute.For<IReviewRepository>();
    private readonly ModerateReviewCommandHandler handler;

    public ModerateReviewCommandHandlerTests()
    {
        handler = new ModerateReviewCommandHandler(reviewRepository, new FixedTimeProvider(KnownInstantUtc));
    }

    [Fact]
    public async Task ThrowsNotFoundWhenTheReviewDoesNotExist()
    {
        reviewRepository.GetByIdAsync(ReviewId, Arg.Any<CancellationToken>()).Returns((ProductReview?)null);

        var moderating = () => handler.Handle(
            new ModerateReviewCommand(ReviewId, ModerationDecision.Approved, ModeratorId),
            CancellationToken.None);

        await moderating.Should().ThrowAsync<NotFoundException>().WithMessage($"*{ReviewId}*");
        await reviewRepository.DidNotReceive().ReplaceAsync(Arg.Any<ProductReview>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApprovingAPendingReviewMovesItToApprovedAndStampsTheModerator()
    {
        var review = PendingReview();
        reviewRepository.GetByIdAsync(ReviewId, Arg.Any<CancellationToken>()).Returns(review);

        var commandResult = await handler.Handle(
            new ModerateReviewCommand(ReviewId, ModerationDecision.Approved, ModeratorId),
            CancellationToken.None);

        commandResult.Status.Should().Be(ReviewStatus.Approved);
        commandResult.ModeratedOnUtc.Should().Be(KnownInstantUtc);
        review.Status.Should().Be(ReviewStatus.Approved);
        review.ModeratedBy.Should().Be(ModeratorId);
        review.ModeratedOnUtc.Should().Be(KnownInstantUtc);
        await reviewRepository.Received(1).ReplaceAsync(review, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectingAPendingReviewMovesItToRejected()
    {
        var review = PendingReview();
        reviewRepository.GetByIdAsync(ReviewId, Arg.Any<CancellationToken>()).Returns(review);

        var commandResult = await handler.Handle(
            new ModerateReviewCommand(ReviewId, ModerationDecision.Rejected, ModeratorId),
            CancellationToken.None);

        commandResult.Status.Should().Be(ReviewStatus.Rejected);
        review.Status.Should().Be(ReviewStatus.Rejected);
    }

    [Fact]
    public async Task ReModeratingAnAlreadyApprovedReviewToRejectedLetsTheLastDecisionWin()
    {
        var review = PendingReview();
        review.ApplyModeration(ModerationDecision.Approved, ModeratorId, CreatedOnUtc.AddHours(1));
        reviewRepository.GetByIdAsync(ReviewId, Arg.Any<CancellationToken>()).Returns(review);

        var commandResult = await handler.Handle(
            new ModerateReviewCommand(ReviewId, ModerationDecision.Rejected, ModeratorId),
            CancellationToken.None);

        commandResult.Status.Should().Be(ReviewStatus.Rejected);
        review.Status.Should().Be(ReviewStatus.Rejected);
        review.ModeratedOnUtc.Should().Be(KnownInstantUtc);
    }

    [Fact]
    public async Task ApplyingTheSameDecisionTwiceLeavesTheReviewInTheSameTerminalState()
    {
        var review = PendingReview();
        review.ApplyModeration(ModerationDecision.Approved, ModeratorId, CreatedOnUtc.AddHours(1));
        reviewRepository.GetByIdAsync(ReviewId, Arg.Any<CancellationToken>()).Returns(review);

        var commandResult = await handler.Handle(
            new ModerateReviewCommand(ReviewId, ModerationDecision.Approved, ModeratorId),
            CancellationToken.None);

        commandResult.Status.Should().Be(ReviewStatus.Approved);
        review.Status.Should().Be(ReviewStatus.Approved);
    }

    private static ProductReview PendingReview() => ProductReview.Submit(
        ReviewId,
        "FC-PRD-0001",
        CustomerId,
        "Dana Customer",
        rating: 5,
        title: "Excellent product",
        body: "It arrived quickly and works exactly as described.",
        isVerifiedPurchase: true,
        createdOnUtc: CreatedOnUtc);
}
