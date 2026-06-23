using FreshCart.Basket.Api.Domain;

namespace FreshCart.Basket.Api.Pricing;

public sealed record BasketPricingRequest(
    Guid CustomerId,
    string? CouponCode,
    string CurrencyCode,
    IReadOnlyList<BasketPricingLine> Lines)
{
    public static BasketPricingRequest ForBasket(ShoppingBasket basket)
    {
        ArgumentNullException.ThrowIfNull(basket);

        return new BasketPricingRequest(
            basket.Id,
            basket.CouponCode,
            basket.CurrencyCode,
            [.. basket.Items.Select(item => new BasketPricingLine(item.ProductId, item.ProductSku, item.UnitPrice, item.Quantity))]);
    }
}
