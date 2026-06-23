using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Features.Reviews.CreateReview;
using FreshCart.Reviews.Api.Persistence;
using FreshCart.Reviews.Tests.TestInfrastructure;
using NSubstitute;

namespace FreshCart.Reviews.Tests.Reviews;

public sealed class CreateReviewCommandHandlerTests
{
    private static readonly DateTimeOffset KnownInstantUtc = new(2026, 6, 18, 9, 15, 0, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    private const string ProductSku = "FC-PRD-0001";
    private const string DisplayName = "Dana Customer";

    private readonly IReviewRepository reviewRepository = Substitute.For<IReviewRepository>();
    private readonly IPurchaseRecordRepository purchaseRecordRepository = Substitute.For<IPurchaseRecordRepository>();
    private readonly CreateReviewCommandHandler handler;

    public CreateReviewCommandHandlerTests()
    {
        handler = new CreateReviewCommandHandler(
            reviewRepository,
            purchaseRecordRepository,
            new FixedTimeProvider(KnownInstantUtc));
    }

    [Fact]
    public async Task RejectsASecondReviewOfTheSameProductWithConflictBeforeWritingAnything()
    {
        var command = CreateCommand();
        reviewRepository.ExistsForCustomerAsync(ProductSku, CustomerId, Arg.Any<CancellationToken>()).Returns(true);

        var creating = () => handler.Handle(command, CancellationToken.None);

        await creating.Should().ThrowAsync<ConflictException>().WithMessage($"*{ProductSku}*");
        await reviewRepository.DidNotReceive().InsertAsync(Arg.Any<ProductReview>(), Arg.Any<CancellationToken>());
        await purchaseRecordRepository.DidNotReceive()
            .HasPurchasedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlagsTheReviewAsVerifiedWhenAMatchingPurchaseRecordExists()
    {
        var command = CreateCommand();
        reviewRepository.ExistsForCustomerAsync(ProductSku, CustomerId, Arg.Any<CancellationToken>()).Returns(false);
        purchaseRecordRepository.HasPurchasedAsync(CustomerId, ProductSku, Arg.Any<CancellationToken>()).Returns(true);

        var commandResult = await handler.Handle(command, CancellationToken.None);

        commandResult.IsVerifiedPurchase.Should().BeTrue();
    }

    [Fact]
    public async Task LeavesTheReviewUnverifiedWhenNoPurchaseRecordExists()
    {
        var command = CreateCommand();
        reviewRepository.ExistsForCustomerAsync(ProductSku, CustomerId, Arg.Any<CancellationToken>()).Returns(false);
        purchaseRecordRepository.HasPurchasedAsync(CustomerId, ProductSku, Arg.Any<CancellationToken>()).Returns(false);

        var commandResult = await handler.Handle(command, CancellationToken.None);

        commandResult.IsVerifiedPurchase.Should().BeFalse();
    }

    [Fact]
    public async Task StoresAPendingReviewStampedWithTheInjectedClockAndAuthorFromTheCommand()
    {
        var command = CreateCommand();
        reviewRepository.ExistsForCustomerAsync(ProductSku, CustomerId, Arg.Any<CancellationToken>()).Returns(false);
        purchaseRecordRepository.HasPurchasedAsync(CustomerId, ProductSku, Arg.Any<CancellationToken>()).Returns(true);
        ProductReview? storedReview = null;
        await reviewRepository
            .InsertAsync(Arg.Do<ProductReview>(review => storedReview = review), Arg.Any<CancellationToken>());

        var commandResult = await handler.Handle(command, CancellationToken.None);

        storedReview.Should().NotBeNull();
        storedReview!.Status.Should().Be(ReviewStatus.Pending);
        storedReview.CustomerId.Should().Be(CustomerId);
        storedReview.CustomerDisplayName.Should().Be(DisplayName);
        storedReview.CreatedOnUtc.Should().Be(KnownInstantUtc);
        storedReview.ModeratedOnUtc.Should().BeNull();
        storedReview.ModeratedBy.Should().BeNull();
        storedReview.IsVerifiedPurchase.Should().BeTrue();
        commandResult.ReviewId.Should().Be(storedReview.Id);
        commandResult.Status.Should().Be(ReviewStatus.Pending);
    }

    [Fact]
    public async Task TrimsTheTitleAndBodyBeforeStoringThem()
    {
        var command = CreateCommand() with { Title = "  Great pick  ", Body = "  Genuinely useful purchase.  " };
        reviewRepository.ExistsForCustomerAsync(ProductSku, CustomerId, Arg.Any<CancellationToken>()).Returns(false);
        ProductReview? storedReview = null;
        await reviewRepository
            .InsertAsync(Arg.Do<ProductReview>(review => storedReview = review), Arg.Any<CancellationToken>());

        await handler.Handle(command, CancellationToken.None);

        storedReview!.Title.Should().Be("Great pick");
        storedReview.Body.Should().Be("Genuinely useful purchase.");
    }

    private static CreateReviewCommand CreateCommand() => new(
        ProductSku,
        CustomerId,
        DisplayName,
        Rating: 5,
        Title: "Excellent product",
        Body: "It arrived quickly and works exactly as described.");
}
