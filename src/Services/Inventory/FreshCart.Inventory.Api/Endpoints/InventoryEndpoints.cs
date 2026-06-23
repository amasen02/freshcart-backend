using Carter;
using FluentValidation;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Inventory.Api.Services;

namespace FreshCart.Inventory.Api.Endpoints;

public sealed class InventoryEndpoints : ICarterModule
{
    private const int MaxProductSkuLength = 50;

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var inventoryGroup = app.MapGroup("/inventory").WithTags("Inventory");

        inventoryGroup.MapGet("/", GetStockItemsPageAsync)
            .RequireAuthorization(AuthorizationPolicies.BackOfficeUser)
            .WithSummary("Stock levels, paginated and ordered by sku.")
            .Produces<PaginatedResult<StockItemResponse>>(StatusCodes.Status200OK);

        inventoryGroup.MapGet("/{productSku}", GetStockItemAsync)
            .RequireAuthorization(AuthorizationPolicies.BackOfficeUser)
            .WithSummary("Stock level for a single sku.")
            .Produces<StockItemResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        inventoryGroup.MapPut("/{productSku}", UpsertStockItemAsync)
            .RequireAuthorization(AuthorizationPolicies.Administrator)
            .WithSummary("Create or update the stock row for a sku. Reserved quantity is never overwritten.")
            .Produces<StockItemResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetStockItemsPageAsync(
        [AsParameters] PaginationRequest paginationRequest,
        IStockLevelService stockLevelService,
        CancellationToken cancellationToken)
    {
        var stockItemsPage = await stockLevelService
            .GetStockItemsPageAsync(paginationRequest, cancellationToken)
            .ConfigureAwait(false);

        var responsePage = new PaginatedResult<StockItemResponse>(
            stockItemsPage.PageNumber,
            stockItemsPage.PageSize,
            stockItemsPage.TotalItemCount,
            stockItemsPage.Items.Select(StockItemResponse.FromStockItem).ToList());

        return Results.Ok(responsePage);
    }

    private static async Task<IResult> GetStockItemAsync(
        string productSku,
        IStockLevelService stockLevelService,
        CancellationToken cancellationToken)
    {
        EnsureValidProductSku(productSku);

        var stockItem = await stockLevelService
            .GetStockItemAsync(productSku, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(StockItemResponse.FromStockItem(stockItem));
    }

    private static async Task<IResult> UpsertStockItemAsync(
        string productSku,
        UpsertStockItemRequest upsertRequest,
        IValidator<UpsertStockItemRequest> requestValidator,
        IStockLevelService stockLevelService,
        CancellationToken cancellationToken)
    {
        EnsureValidProductSku(productSku);
        await requestValidator.ValidateAndThrowAsync(upsertRequest, cancellationToken).ConfigureAwait(false);

        var stockItem = await stockLevelService
            .SetStockLevelAsync(productSku, upsertRequest.ProductName, upsertRequest.QuantityOnHand, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(StockItemResponse.FromStockItem(stockItem));
    }

    private static void EnsureValidProductSku(string productSku)
    {
        if (string.IsNullOrWhiteSpace(productSku) || productSku.Length > MaxProductSkuLength)
        {
            throw new BadRequestException($"Product sku must be 1 to {MaxProductSkuLength} characters.");
        }
    }
}
