using Carter;
using FreshCart.Catalog.Api.Authentication;
using MediatR;

namespace FreshCart.Catalog.Api.Features.Categories.CreateCategory;

public sealed class CreateCategoryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(CatalogEndpointConventions.CategoriesRoute, CreateCategoryAsync)
            .RequireAuthorization(AuthorizationPolicies.CatalogManager)
            .WithTags(CatalogEndpointConventions.CategoriesTag)
            .WithSummary("Create a category and evict the cached storefront tree.")
            .Produces<CreateCategoryResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> CreateCategoryAsync(
        CreateCategoryCommand command,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var commandResult = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.Created($"{CatalogEndpointConventions.CategoriesRoute}/{commandResult.CategoryId}", commandResult);
    }
}
