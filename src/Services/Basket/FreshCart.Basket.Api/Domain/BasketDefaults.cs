namespace FreshCart.Basket.Api.Domain;

/// <summary>
/// Domain constants shared across basket slices. The default currency applies until multi-currency
/// pricing arrives; the Pricing service is the authority for everything money-related beyond it.
/// </summary>
public static class BasketDefaults
{
    public const string CurrencyCode = "USD";

    // Flat rate until a Delivery-quoted shipping integration exists; digital-only baskets ship free.
    public static readonly decimal StandardShippingFee = 5.99m;
}
