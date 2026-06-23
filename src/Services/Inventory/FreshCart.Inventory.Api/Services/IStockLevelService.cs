using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Inventory.Api.Models;

namespace FreshCart.Inventory.Api.Services;

public interface IStockLevelService
{
    Task<StockItem> GetStockItemAsync(string productSku, CancellationToken cancellationToken);

    Task<PaginatedResult<StockItem>> GetStockItemsPageAsync(PaginationRequest pagination, CancellationToken cancellationToken);

    Task<StockItem> SetStockLevelAsync(
        string productSku,
        string productName,
        int quantityOnHand,
        CancellationToken cancellationToken);

    /// <summary>
    /// Idempotently creates the stock row for a newly catalogued product: inserts it with the initial
    /// quantity on first sight and is a no-op if it already exists. Returns whether a row was created.
    /// </summary>
    Task<bool> EnsureStockItemAsync(
        string productSku,
        string productName,
        int initialQuantityOnHand,
        CancellationToken cancellationToken);
}
