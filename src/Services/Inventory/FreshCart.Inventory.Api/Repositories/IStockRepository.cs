using System.Data;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Inventory.Api.Models;

namespace FreshCart.Inventory.Api.Repositories;

public interface IStockRepository
{
    Task<StockItem?> GetBySkuAsync(string productSku, IDbTransaction? transaction, CancellationToken cancellationToken);

    Task<PaginatedResult<StockItem>> GetPagedAsync(PaginationRequest pagination, CancellationToken cancellationToken);

    Task<IReadOnlyList<StockItem>> GetBySkusWithUpdateLockAsync(
        IReadOnlyCollection<string> productSkus,
        IDbTransaction transaction,
        CancellationToken cancellationToken);

    Task UpsertAsync(StockItem stockItem, IDbTransaction? transaction, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically inserts the stock row only when its sku is not already present, returning whether a row
    /// was created. Unlike <see cref="UpsertAsync"/> it never overwrites an existing row, so a redelivered
    /// <c>ProductCreated</c> event cannot reset the on-hand quantity.
    /// </summary>
    Task<bool> EnsureExistsAsync(StockItem stockItem, IDbTransaction? transaction, CancellationToken cancellationToken);

    Task AdjustQuantityAsync(
        string productSku,
        int quantityOnHandDelta,
        int quantityReservedDelta,
        DateTimeOffset updatedOnUtc,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);
}
