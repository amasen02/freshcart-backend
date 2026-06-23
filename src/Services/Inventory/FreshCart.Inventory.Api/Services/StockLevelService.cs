using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Inventory.Api.Models;
using FreshCart.Inventory.Api.Repositories;
using Microsoft.Data.SqlClient;

namespace FreshCart.Inventory.Api.Services;

public sealed class StockLevelService(IStockRepository stockRepository, TimeProvider timeProvider) : IStockLevelService
{
    private const string StockItemEntityName = "Stock item";
    private const int SqlServerConstraintConflictNumber = 547;

    public async Task<StockItem> GetStockItemAsync(string productSku, CancellationToken cancellationToken)
    {
        var stockItem = await stockRepository
            .GetBySkuAsync(productSku, transaction: null, cancellationToken)
            .ConfigureAwait(false);

        return stockItem ?? throw new NotFoundException(StockItemEntityName, productSku);
    }

    public Task<PaginatedResult<StockItem>> GetStockItemsPageAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        return stockRepository.GetPagedAsync(pagination.Normalise(), cancellationToken);
    }

    public async Task<StockItem> SetStockLevelAsync(
        string productSku,
        string productName,
        int quantityOnHand,
        CancellationToken cancellationToken)
    {
        var stockItem = new StockItem
        {
            ProductSku = productSku,
            ProductName = productName,
            QuantityOnHand = quantityOnHand,
            UpdatedOnUtc = timeProvider.GetUtcNow(),
        };

        try
        {
            await stockRepository.UpsertAsync(stockItem, transaction: null, cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException sqlException) when (sqlException.Number == SqlServerConstraintConflictNumber)
        {
            throw new BadRequestException(
                "Quantity on hand cannot be set below the quantity currently reserved.",
                $"Sku \"{productSku}\" has active reservations that exceed the requested quantity of {quantityOnHand}.");
        }

        return await GetStockItemAsync(productSku, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> EnsureStockItemAsync(
        string productSku,
        string productName,
        int initialQuantityOnHand,
        CancellationToken cancellationToken)
    {
        var stockItem = new StockItem
        {
            ProductSku = productSku,
            ProductName = productName,
            QuantityOnHand = initialQuantityOnHand,
            UpdatedOnUtc = timeProvider.GetUtcNow(),
        };

        return stockRepository.EnsureExistsAsync(stockItem, transaction: null, cancellationToken);
    }
}
