using Carter;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Reviews.Api.Authentication;
using FreshCart.Reviews.Api.Features;
using FreshCart.Reviews.Api.Features.Reviews;
using MediatR;

namespace FreshCart.Reviews.Api.Features.Reviews.GetPendingReviews;

public sealed class GetPendingReviewsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(ReviewsEndpointConventions.PendingReviewsRoute, GetPendingReviewsAsync)
            .RequireAuthorization(AuthorizationPolicies.BackOfficeUser)
            .WithTags(ReviewsEndpointConventions.ReviewsTag)
            .WithSummary("The moderation queue of reviews awaiting a decision, paginated.")
            .Produces<PaginatedResult<ReviewDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetPendingReviewsAsync(
        ISender mediator,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new GetPendingReviewsQuery(new PaginationRequest(pageNumber, pageSize));
        var pendingPage = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Results.Ok(pendingPage);
    }
}
