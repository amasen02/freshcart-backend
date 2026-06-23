namespace FreshCart.Inventory.Api.Models;

public sealed class StockItem
{
    public string ProductSku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int QuantityOnHand { get; set; }

    public int QuantityReserved { get; set; }

    public DateTimeOffset UpdatedOnUtc { get; set; }

    public int QuantityAvailable => QuantityOnHand - QuantityReserved;
}
