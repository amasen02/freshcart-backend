namespace FreshCart.Basket.Api.Domain;

/// <summary>
/// The live basket document for one customer. Identified by the customer id so each customer owns
/// exactly one basket; checkout archives the document under the order id and deletes this one.
/// </summary>
public sealed class ShoppingBasket
{
    public const int MaxQuantityPerLine = 99;

    public Guid Id { get; init; }

    public string CurrencyCode { get; init; } = BasketDefaults.CurrencyCode;

    public IList<BasketItem> Items { get; init; } = [];

    public string? CouponCode { get; set; }

    public DateTimeOffset UpdatedOnUtc { get; set; }

    public bool IsEmpty => Items.Count == 0;

    public bool ContainsPhysicalItems => Items.Any(item => !item.IsDigital);

    public decimal StoredSubtotal => Items.Sum(item => item.UnitPrice * item.Quantity);

    public static ShoppingBasket CreateForCustomer(Guid customerId) => new() { Id = customerId };

    public void AddOrMergeItem(BasketItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var existingLine = FindItem(item.ProductId);
        if (existingLine is null)
        {
            item.Quantity = Math.Min(item.Quantity, MaxQuantityPerLine);
            Items.Add(item);
            return;
        }

        existingLine.Quantity = Math.Min(existingLine.Quantity + item.Quantity, MaxQuantityPerLine);
        existingLine.UnitPrice = item.UnitPrice;
    }

    public bool SetItemQuantity(Guid productId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        var existingLine = FindItem(productId);
        if (existingLine is null)
        {
            return false;
        }

        if (quantity == 0)
        {
            Items.Remove(existingLine);
            return true;
        }

        existingLine.Quantity = Math.Min(quantity, MaxQuantityPerLine);
        return true;
    }

    public bool RemoveItem(Guid productId)
    {
        var existingLine = FindItem(productId);
        if (existingLine is null)
        {
            return false;
        }

        Items.Remove(existingLine);
        return true;
    }

    private BasketItem? FindItem(Guid productId) => Items.FirstOrDefault(item => item.ProductId == productId);
}
