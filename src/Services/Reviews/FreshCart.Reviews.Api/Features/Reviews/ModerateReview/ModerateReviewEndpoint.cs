using System.Security.Claims;
using Carter;
using FreshCart.Reviews.Api.Authentication;
using FreshCart.Reviews.Api.Features;
using MediatR;

namespace FreshCart.Reviews.Api.Features.Reviews.ModerateReview;

public sealed class ModerateReviewEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut(ReviewsEndpointConventions.ModerationRoute, ModerateReviewAsync)
            .RequireAuthorization(AuthorizationPolicies.BackOfficeUser)
            .WithTags(ReviewsEndpointConventions.ReviewsTag)
            .WithSummary("Approve or reject a review. Re-moderation is allowed; the last decision wins.")
            .Produces<ModerateReviewResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ModerateReviewAsync(
        Guid reviewId,
        ModerateReviewRequest request,
        ClaimsPrincipal claimsPrincipal,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new ModerateReviewCommand(reviewId, request.Decision, claimsPrincipal.GetCustomerId());
        var commandResult = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.Ok(commandResult);
    }
}
