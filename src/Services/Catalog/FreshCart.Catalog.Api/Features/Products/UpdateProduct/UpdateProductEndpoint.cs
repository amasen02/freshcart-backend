using Carter;
using FreshCart.Catalog.Api.Authentication;
using MediatR;

namespace FreshCart.Catalog.Api.Features.Products.UpdateProduct;

public sealed class UpdateProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut(CatalogEndpointConventions.ProductByIdRoute, UpdateProductAsync)
            .RequireAuthorization(AuthorizationPolicies.CatalogManager)
            .WithTags(CatalogEndpointConventions.ProductsTag)
            .WithSummary("Replace the editable fields of a product; publishes ProductPriceChanged when the price moves.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UpdateProductAsync(
        Guid productId,
        UpdateProductRequest updateRequest,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(updateRequest);

        var command = new UpdateProductCommand(
            productId,
            updateRequest.Name,
            updateRequest.Description,
            updateRequest.BasePrice,
            updateRequest.CurrencyCode,
            updateRequest.CategoryId,
            updateRequest.BrandId,
            updateRequest.IsDigital,
            updateRequest.IsActive,
            updateRequest.Images,
            updateRequest.Attributes);

        await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
