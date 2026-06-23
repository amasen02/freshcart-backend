namespace FreshCart.Inventory.Api.Models;

public sealed class StockReservationLine
{
    public string ProductSku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
