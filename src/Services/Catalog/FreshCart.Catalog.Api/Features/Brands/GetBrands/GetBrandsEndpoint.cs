using Carter;
using MediatR;

namespace FreshCart.Catalog.Api.Features.Brands.GetBrands;

public sealed class GetBrandsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(CatalogEndpointConventions.BrandsRoute, GetBrandsAsync)
            .AllowAnonymous()
            .WithTags(CatalogEndpointConventions.BrandsTag)
            .WithSummary("Active brands ordered by name.")
            .Produces<IReadOnlyList<BrandDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetBrandsAsync(
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var brands = await mediator.Send(new GetBrandsQuery(), cancellationToken).ConfigureAwait(false);
        return Results.Ok(brands);
    }
}
