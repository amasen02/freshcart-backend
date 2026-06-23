using Carter;
using FreshCart.BuildingBlocks.Exceptions;
using MediatR;

namespace FreshCart.Catalog.Api.Features.Products.GetProduct;

public sealed class GetProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(CatalogEndpointConventions.ProductByIdOrSlugRoute, GetProductAsync)
            .AllowAnonymous()
            .WithTags(CatalogEndpointConventions.ProductsTag)
            .WithSummary("Single product by id or slug, served from HybridCache for five minutes.")
            .Produces<ProductDetailsDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetProductAsync(
        string idOrSlug,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idOrSlug) || idOrSlug.Length > ProductConstraints.MaxIdOrSlugLength)
        {
            throw new BadRequestException($"The product identifier must be 1 to {ProductConstraints.MaxIdOrSlugLength} characters.");
        }

        var productDetails = await mediator.Send(new GetProductQuery(idOrSlug), cancellationToken).ConfigureAwait(false);
        return Results.Ok(productDetails);
    }
}
