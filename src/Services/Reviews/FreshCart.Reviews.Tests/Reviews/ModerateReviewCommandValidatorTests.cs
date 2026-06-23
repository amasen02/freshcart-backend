using FluentValidation.TestHelper;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Features.Reviews.ModerateReview;

namespace FreshCart.Reviews.Tests.Reviews;

public sealed class ModerateReviewCommandValidatorTests
{
    private static readonly Guid ReviewId = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    private static readonly Guid ModeratorId = Guid.Parse("e0000000-0000-0000-0000-000000000001");

    private readonly ModerateReviewCommandValidator validator = new();

    [Theory]
    [InlineData(ModerationDecision.Approved)]
    [InlineData(ModerationDecision.Rejected)]
    public void AcceptsAValidDecision(ModerationDecision decision)
    {
        var command = new ModerateReviewCommand(ReviewId, decision, ModeratorId);

        validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RejectsAnEmptyReviewId()
    {
        var command = new ModerateReviewCommand(Guid.Empty, ModerationDecision.Approved, ModeratorId);

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.ReviewId);
    }

    [Fact]
    public void RejectsAnEmptyModeratorId()
    {
        var command = new ModerateReviewCommand(ReviewId, ModerationDecision.Approved, Guid.Empty);

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.ModeratorId);
    }

    [Fact]
    public void RejectsADecisionOutsideTheDefinedSet()
    {
        var command = new ModerateReviewCommand(ReviewId, (ModerationDecision)99, ModeratorId);

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Decision);
    }
}
