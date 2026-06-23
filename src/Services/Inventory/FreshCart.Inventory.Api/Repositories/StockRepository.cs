using System.Data;
using Dapper;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Inventory.Api.Models;

namespace FreshCart.Inventory.Api.Repositories;

public sealed class StockRepository(ISqlConnectionFactory connectionFactory) : IStockRepository
{
    private const string SelectColumns = "ProductSku, ProductName, QuantityOnHand, QuantityReserved, UpdatedOnUtc";

    public async Task<StockItem?> GetBySkuAsync(
        string productSku,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string selectBySkuSql = $"""
            SELECT {SelectColumns}
            FROM dbo.stock_items
            WHERE ProductSku = @ProductSku;
            """;

        var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        return await connection.QuerySingleOrDefaultAsync<StockItem>(new CommandDefinition(
                selectBySkuSql,
                new { ProductSku = productSku },
                transaction,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<PaginatedResult<StockItem>> GetPagedAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        const string selectPageSql = $"""
            SELECT COUNT_BIG(*) FROM dbo.stock_items;

            SELECT {SelectColumns}
            FROM dbo.stock_items
            ORDER BY ProductSku
            OFFSET @RowOffset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var pageParameters = new
        {
            RowOffset = (pagination.PageNumber - 1) * pagination.PageSize,
            pagination.PageSize,
        };

        var resultSets = await connection.QueryMultipleAsync(new CommandDefinition(
                selectPageSql,
                pageParameters,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        await using (resultSets.ConfigureAwait(false))
        {
            var totalItemCount = await resultSets.ReadSingleAsync<long>().ConfigureAwait(false);
            var stockItems = await resultSets.ReadAsync<StockItem>().ConfigureAwait(false);

            return new PaginatedResult<StockItem>(
                pagination.PageNumber,
                pagination.PageSize,
                totalItemCount,
                stockItems.ToList());
        }
    }

    public async Task<IReadOnlyList<StockItem>> GetBySkusWithUpdateLockAsync(
        IReadOnlyCollection<string> productSkus,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(productSkus);
        ArgumentNullException.ThrowIfNull(transaction);

        const string selectForUpdateSql = $"""
            SELECT {SelectColumns}
            FROM dbo.stock_items WITH (UPDLOCK, ROWLOCK)
            WHERE ProductSku IN @ProductSkus
            ORDER BY ProductSku;
            """;

        var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var lockedItems = await connection.QueryAsync<StockItem>(new CommandDefinition(
                selectForUpdateSql,
                new { ProductSkus = productSkus },
                transaction,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return lockedItems.ToList();
    }

    public async Task UpsertAsync(StockItem stockItem, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stockItem);

        const string upsertSql = """
            MERGE dbo.stock_items WITH (HOLDLOCK) AS target
            USING (SELECT @ProductSku AS ProductSku) AS source
            ON target.ProductSku = source.ProductSku
            WHEN MATCHED THEN
                UPDATE SET ProductName = @ProductName,
                           QuantityOnHand = @QuantityOnHand,
                           UpdatedOnUtc = @UpdatedOnUtc
            WHEN NOT MATCHED THEN
                INSERT (ProductSku, ProductName, QuantityOnHand, QuantityReserved, UpdatedOnUtc)
                VALUES (@ProductSku, @ProductName, @QuantityOnHand, 0, @UpdatedOnUtc);
            """;

        var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var upsertParameters = new
        {
            stockItem.ProductSku,
            stockItem.ProductName,
            stockItem.QuantityOnHand,
            stockItem.UpdatedOnUtc,
        };

        await connection.ExecuteAsync(new CommandDefinition(
                upsertSql,
                upsertParameters,
                transaction,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<bool> EnsureExistsAsync(StockItem stockItem, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stockItem);

        const string insertIfAbsentSql = """
            MERGE dbo.stock_items WITH (HOLDLOCK) AS target
            USING (SELECT @ProductSku AS ProductSku) AS source
            ON target.ProductSku = source.ProductSku
            WHEN NOT MATCHED THEN
                INSERT (ProductSku, ProductName, QuantityOnHand, QuantityReserved, UpdatedOnUtc)
                VALUES (@ProductSku, @ProductName, @QuantityOnHand, 0, @UpdatedOnUtc);
            """;

        var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var insertParameters = new
        {
            stockItem.ProductSku,
            stockItem.ProductName,
            stockItem.QuantityOnHand,
            stockItem.UpdatedOnUtc,
        };

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
                insertIfAbsentSql,
                insertParameters,
                transaction,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return rowsAffected > 0;
    }

    public async Task AdjustQuantityAsync(
        string productSku,
        int quantityOnHandDelta,
        int quantityReservedDelta,
        DateTimeOffset updatedOnUtc,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string adjustQuantitySql = """
            UPDATE dbo.stock_items
            SET QuantityOnHand = QuantityOnHand + @QuantityOnHandDelta,
                QuantityReserved = QuantityReserved + @QuantityReservedDelta,
                UpdatedOnUtc = @UpdatedOnUtc
            WHERE ProductSku = @ProductSku;
            """;

        var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var adjustmentParameters = new
        {
            ProductSku = productSku,
            QuantityOnHandDelta = quantityOnHandDelta,
            QuantityReservedDelta = quantityReservedDelta,
            UpdatedOnUtc = updatedOnUtc,
        };

        await connection.ExecuteAsync(new CommandDefinition(
                adjustQuantitySql,
                adjustmentParameters,
                transaction,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }
}
