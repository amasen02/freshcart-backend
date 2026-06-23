using Carter;
using FreshCart.Catalog.Api.Authentication;
using MediatR;

namespace FreshCart.Catalog.Api.Features.Brands.CreateBrand;

public sealed class CreateBrandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(CatalogEndpointConventions.BrandsRoute, CreateBrandAsync)
            .RequireAuthorization(AuthorizationPolicies.CatalogManager)
            .WithTags(CatalogEndpointConventions.BrandsTag)
            .WithSummary("Create a brand.")
            .Produces<CreateBrandResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> CreateBrandAsync(
        CreateBrandCommand command,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var commandResult = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.Created($"{CatalogEndpointConventions.BrandsRoute}/{commandResult.BrandId}", commandResult);
    }
}
