using FluentValidation;
using FreshCart.Reviews.Api.Domain;

namespace FreshCart.Reviews.Api.Features.Reviews.ModerateReview;

public sealed class ModerateReviewCommandValidator : AbstractValidator<ModerateReviewCommand>
{
    public ModerateReviewCommandValidator()
    {
        RuleFor(command => command.ReviewId)
            .NotEmpty();

        RuleFor(command => command.ModeratorId)
            .NotEmpty();

        RuleFor(command => command.Decision)
            .Must(decision => decision is ModerationDecision.Approved or ModerationDecision.Rejected)
            .WithMessage("Decision must be either Approved or Rejected.");
    }
}
