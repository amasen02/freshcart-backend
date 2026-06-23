using Carter;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Pagination;
using MediatR;

namespace FreshCart.Catalog.Api.Features.Products.SearchProducts;

public sealed class SearchProductsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(CatalogEndpointConventions.ProductsSearchRoute, SearchProductsAsync)
            .AllowAnonymous()
            .WithTags(CatalogEndpointConventions.ProductsTag)
            .WithSummary("Full-text search over active products by name and description.")
            .Produces<PaginatedResult<ProductSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> SearchProductsAsync(
        string? term,
        ISender mediator,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            throw new BadRequestException("A search term is required.");
        }

        var searchTerm = term.Trim();
        if (searchTerm.Length > ProductConstraints.MaxSearchTermLength)
        {
            throw new BadRequestException($"The search term cannot exceed {ProductConstraints.MaxSearchTermLength} characters.");
        }

        var query = new SearchProductsQuery(searchTerm, new PaginationRequest(pageNumber, pageSize));
        var matchesPage = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Results.Ok(matchesPage);
    }
}
