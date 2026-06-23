using Carter;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Features;
using MediatR;

namespace FreshCart.Reviews.Api.Features.Reviews.GetProductReviews;

public sealed class GetProductReviewsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(ReviewsEndpointConventions.ProductReviewsRoute, GetProductReviewsAsync)
            .AllowAnonymous()
            .WithTags(ReviewsEndpointConventions.ReviewsTag)
            .WithSummary("Approved reviews for a product with the aggregate rating summary, paginated.")
            .Produces<ProductReviewsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetProductReviewsAsync(
        string productSku,
        ISender mediator,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(productSku))
        {
            throw new BadRequestException("A product sku is required.");
        }

        if (productSku.Length > ReviewConstraints.MaxProductSkuLength)
        {
            throw new BadRequestException($"productSku must be at most {ReviewConstraints.MaxProductSkuLength} characters.");
        }

        var query = new GetProductReviewsQuery(productSku, new PaginationRequest(pageNumber, pageSize));
        var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Results.Ok(response);
    }
}
