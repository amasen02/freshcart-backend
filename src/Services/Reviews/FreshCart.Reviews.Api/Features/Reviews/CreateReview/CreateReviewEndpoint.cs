using System.Security.Claims;
using Carter;
using FreshCart.Reviews.Api.Authentication;
using FreshCart.Reviews.Api.Features;
using MediatR;

namespace FreshCart.Reviews.Api.Features.Reviews.CreateReview;

public sealed class CreateReviewEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(ReviewsEndpointConventions.ReviewsRoute, CreateReviewAsync)
            .RequireAuthorization(AuthorizationPolicies.Customer)
            .WithTags(ReviewsEndpointConventions.ReviewsTag)
            .WithSummary("Submit a product review. The review starts pending moderation.")
            .Produces<CreateReviewResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> CreateReviewAsync(
        CreateReviewRequest request,
        ClaimsPrincipal claimsPrincipal,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new CreateReviewCommand(
            request.ProductSku,
            claimsPrincipal.GetCustomerId(),
            claimsPrincipal.GetCustomerDisplayName(),
            request.Rating,
            request.Title,
            request.Body);

        var commandResult = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.Created($"{ReviewsEndpointConventions.ReviewsRoute}/{commandResult.ReviewId}", commandResult);
    }
}
