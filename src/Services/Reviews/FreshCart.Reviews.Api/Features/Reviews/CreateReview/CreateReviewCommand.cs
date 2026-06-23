using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Reviews.Api.Features.Reviews.CreateReview;

public sealed record CreateReviewCommand(
    string ProductSku,
    Guid CustomerId,
    string CustomerDisplayName,
    int Rating,
    string Title,
    string Body) : ICommand<CreateReviewResult>;
