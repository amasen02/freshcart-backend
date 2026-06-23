using Carter;
using FreshCart.Catalog.Api.Authentication;
using MediatR;

namespace FreshCart.Catalog.Api.Features.Products.CreateProduct;

public sealed class CreateProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(CatalogEndpointConventions.ProductsRoute, CreateProductAsync)
            .RequireAuthorization(AuthorizationPolicies.CatalogManager)
            .WithTags(CatalogEndpointConventions.ProductsTag)
            .WithSummary("Create a product and publish ProductCreated so Inventory seeds its stock row.")
            .Produces<CreateProductResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> CreateProductAsync(
        CreateProductCommand command,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var commandResult = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.Created($"{CatalogEndpointConventions.ProductsRoute}/{commandResult.ProductId}", commandResult);
    }
}
