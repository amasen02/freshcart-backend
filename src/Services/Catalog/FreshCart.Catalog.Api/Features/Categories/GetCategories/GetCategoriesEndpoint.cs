using Carter;
using MediatR;

namespace FreshCart.Catalog.Api.Features.Categories.GetCategories;

public sealed class GetCategoriesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(CatalogEndpointConventions.CategoriesRoute, GetCategoriesAsync)
            .AllowAnonymous()
            .WithTags(CatalogEndpointConventions.CategoriesTag)
            .WithSummary("Nested category tree ordered by sort order, served from HybridCache for ten minutes.")
            .Produces<IReadOnlyList<CategoryNodeDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetCategoriesAsync(
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var categoryTree = await mediator.Send(new GetCategoriesQuery(), cancellationToken).ConfigureAwait(false);
        return Results.Ok(categoryTree);
    }
}
