using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reviews.Api.Domain;

namespace FreshCart.Reviews.Api.Features.Reviews.ModerateReview;

public sealed record ModerateReviewCommand(Guid ReviewId, ModerationDecision Decision, Guid ModeratorId)
    : ICommand<ModerateReviewResult>;
