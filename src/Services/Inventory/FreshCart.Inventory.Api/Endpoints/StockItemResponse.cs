using FreshCart.Inventory.Api.Models;

namespace FreshCart.Inventory.Api.Endpoints;

public sealed record StockItemResponse(
    string ProductSku,
    string ProductName,
    int QuantityOnHand,
    int QuantityReserved,
    int QuantityAvailable,
    DateTimeOffset UpdatedOnUtc)
{
    public static StockItemResponse FromStockItem(StockItem stockItem)
    {
        ArgumentNullException.ThrowIfNull(stockItem);

        return new StockItemResponse(
            stockItem.ProductSku,
            stockItem.ProductName,
            stockItem.QuantityOnHand,
            stockItem.QuantityReserved,
            stockItem.QuantityAvailable,
            stockItem.UpdatedOnUtc);
    }
}
