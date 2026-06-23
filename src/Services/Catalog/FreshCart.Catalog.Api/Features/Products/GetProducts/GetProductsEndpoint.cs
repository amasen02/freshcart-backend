using System.Security.Claims;
using Carter;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Catalog.Api.Authentication;
using MediatR;

namespace FreshCart.Catalog.Api.Features.Products.GetProducts;

public sealed class GetProductsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(CatalogEndpointConventions.ProductsRoute, GetProductsAsync)
            .AllowAnonymous()
            .WithTags(CatalogEndpointConventions.ProductsTag)
            .WithSummary("Paginated product listing with category, brand, price and digital filters.")
            .Produces<PaginatedResult<ProductSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetProductsAsync(
        [AsParameters] GetProductsRequest listingRequest,
        ClaimsPrincipal claimsPrincipal,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listingRequest);

        if (listingRequest.MaxPrice is <= 0)
        {
            throw new BadRequestException("maxPrice must be greater than zero.");
        }

        if (!ProductSortOptionParser.TryParse(listingRequest.Sort, out var sortOption))
        {
            throw new BadRequestException($"Unknown sort \"{listingRequest.Sort}\". Allowed values: {ProductSortOptionParser.AllowedTokensDescription}.");
        }

        var includeInactive = claimsPrincipal.IsInRole(AuthorizationPolicies.AdministratorRoleName)
            || claimsPrincipal.IsInRole(AuthorizationPolicies.ManagerRoleName);

        var query = new GetProductsQuery(
            listingRequest.CategoryId,
            listingRequest.BrandId,
            listingRequest.MaxPrice,
            listingRequest.IsDigital,
            sortOption,
            includeInactive,
            new PaginationRequest(listingRequest.PageNumber, listingRequest.PageSize));

        var productsPage = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Results.Ok(productsPage);
    }
}
