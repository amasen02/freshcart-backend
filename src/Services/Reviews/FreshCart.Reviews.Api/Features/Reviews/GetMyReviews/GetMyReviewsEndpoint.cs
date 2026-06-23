using System.Security.Claims;
using Carter;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Reviews.Api.Authentication;
using FreshCart.Reviews.Api.Features;
using FreshCart.Reviews.Api.Features.Reviews;
using MediatR;

namespace FreshCart.Reviews.Api.Features.Reviews.GetMyReviews;

public sealed class GetMyReviewsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(ReviewsEndpointConventions.MyReviewsRoute, GetMyReviewsAsync)
            .RequireAuthorization(AuthorizationPolicies.Customer)
            .WithTags(ReviewsEndpointConventions.ReviewsTag)
            .WithSummary("The authenticated customer's own reviews in every status, paginated.")
            .Produces<PaginatedResult<ReviewDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetMyReviewsAsync(
        ClaimsPrincipal claimsPrincipal,
        ISender mediator,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new GetMyReviewsQuery(
            claimsPrincipal.GetCustomerId(),
            new PaginationRequest(pageNumber, pageSize));

        var reviewsPage = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Results.Ok(reviewsPage);
    }
}
