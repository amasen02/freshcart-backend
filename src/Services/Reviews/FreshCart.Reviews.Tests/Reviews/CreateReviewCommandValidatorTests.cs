using FluentValidation.TestHelper;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Features.Reviews.CreateReview;

namespace FreshCart.Reviews.Tests.Reviews;

public sealed class CreateReviewCommandValidatorTests
{
    private readonly CreateReviewCommandValidator validator = new();

    [Fact]
    public void AcceptsAFullyPopulatedValidCommand()
    {
        validator.TestValidate(CreateValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RejectsMissingProductSku(string productSku)
    {
        var command = CreateValidCommand() with { ProductSku = productSku };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.ProductSku);
    }

    [Fact]
    public void RejectsProductSkuLongerThanTheLimit()
    {
        var command = CreateValidCommand() with { ProductSku = new string('A', ReviewConstraints.MaxProductSkuLength + 1) };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.ProductSku);
    }

    [Fact]
    public void RejectsAnEmptyCustomerId()
    {
        var command = CreateValidCommand() with { CustomerId = Guid.Empty };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.CustomerId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RejectsMissingDisplayName(string displayName)
    {
        var command = CreateValidCommand() with { CustomerDisplayName = displayName };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.CustomerDisplayName);
    }

    [Theory]
    [InlineData(ReviewConstraints.MinimumRating - 1)]
    [InlineData(ReviewConstraints.MaximumRating + 1)]
    [InlineData(-3)]
    [InlineData(10)]
    public void RejectsRatingsOutsideTheOneToFiveRange(int rating)
    {
        var command = CreateValidCommand() with { Rating = rating };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Rating);
    }

    [Theory]
    [InlineData(ReviewConstraints.MinimumRating)]
    [InlineData(3)]
    [InlineData(ReviewConstraints.MaximumRating)]
    public void AcceptsRatingsInsideTheOneToFiveRange(int rating)
    {
        var command = CreateValidCommand() with { Rating = rating };

        validator.TestValidate(command).ShouldNotHaveValidationErrorFor(valid => valid.Rating);
    }

    [Fact]
    public void RejectsATitleShorterThanTheMinimum()
    {
        var command = CreateValidCommand() with { Title = new string('a', ReviewConstraints.MinTitleLength - 1) };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Title);
    }

    [Fact]
    public void RejectsATitleLongerThanTheMaximum()
    {
        var command = CreateValidCommand() with { Title = new string('a', ReviewConstraints.MaxTitleLength + 1) };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Title);
    }

    [Theory]
    [InlineData(ReviewConstraints.MinTitleLength)]
    [InlineData(ReviewConstraints.MaxTitleLength)]
    public void AcceptsTitlesOnTheLengthBoundaries(int titleLength)
    {
        var command = CreateValidCommand() with { Title = new string('a', titleLength) };

        validator.TestValidate(command).ShouldNotHaveValidationErrorFor(valid => valid.Title);
    }

    [Fact]
    public void RejectsABodyShorterThanTheMinimum()
    {
        var command = CreateValidCommand() with { Body = new string('a', ReviewConstraints.MinBodyLength - 1) };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Body);
    }

    [Fact]
    public void RejectsABodyLongerThanTheMaximum()
    {
        var command = CreateValidCommand() with { Body = new string('a', ReviewConstraints.MaxBodyLength + 1) };

        validator.TestValidate(command).ShouldHaveValidationErrorFor(invalid => invalid.Body);
    }

    [Theory]
    [InlineData(ReviewConstraints.MinBodyLength)]
    [InlineData(ReviewConstraints.MaxBodyLength)]
    public void AcceptsBodiesOnTheLengthBoundaries(int bodyLength)
    {
        var command = CreateValidCommand() with { Body = new string('a', bodyLength) };

        validator.TestValidate(command).ShouldNotHaveValidationErrorFor(valid => valid.Body);
    }

    private static CreateReviewCommand CreateValidCommand() => new(
        ProductSku: "FC-PRD-0001",
        CustomerId: Guid.Parse("c0000000-0000-0000-0000-000000000001"),
        CustomerDisplayName: "Dana Customer",
        Rating: 4,
        Title: "Solid value",
        Body: "Held up well over a month of daily use.");
}
