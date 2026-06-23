using Carter;
using FreshCart.Catalog.Api.Authentication;
using MediatR;

namespace FreshCart.Catalog.Api.Features.Products.DeleteProduct;

public sealed class DeleteProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapDelete(CatalogEndpointConventions.ProductByIdRoute, DeleteProductAsync)
            .RequireAuthorization(AuthorizationPolicies.CatalogManager)
            .WithTags(CatalogEndpointConventions.ProductsTag)
            .WithSummary("Soft delete a product so it disappears from the storefront but stays resolvable.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> DeleteProductAsync(
        Guid productId,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProductCommand(productId), cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
