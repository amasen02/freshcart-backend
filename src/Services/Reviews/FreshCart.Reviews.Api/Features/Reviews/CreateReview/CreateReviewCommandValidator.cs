using FluentValidation;
using FreshCart.Reviews.Api.Domain;

namespace FreshCart.Reviews.Api.Features.Reviews.CreateReview;

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(command => command.ProductSku)
            .NotEmpty()
            .MaximumLength(ReviewConstraints.MaxProductSkuLength);

        RuleFor(command => command.CustomerId)
            .NotEmpty();

        RuleFor(command => command.CustomerDisplayName)
            .NotEmpty();

        RuleFor(command => command.Rating)
            .InclusiveBetween(ReviewConstraints.MinimumRating, ReviewConstraints.MaximumRating);

        RuleFor(command => command.Title)
            .NotEmpty()
            .MinimumLength(ReviewConstraints.MinTitleLength)
            .MaximumLength(ReviewConstraints.MaxTitleLength);

        RuleFor(command => command.Body)
            .NotEmpty()
            .MinimumLength(ReviewConstraints.MinBodyLength)
            .MaximumLength(ReviewConstraints.MaxBodyLength);
    }
}
