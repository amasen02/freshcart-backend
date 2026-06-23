namespace FreshCart.Basket.Api.Features.ShoppingBaskets;

/// <summary>
/// Route fragments and the OpenAPI tag shared by every basket slice, so the slices never drift
/// apart on paths.
/// </summary>
public static class BasketEndpointConventions
{
    public const string Tag = "Basket";

    public const string BasketRoute = "/basket";

    public const string ItemsRoute = "/basket/items";

    public const string ItemRoute = "/basket/items/{productId:guid}";

    public const string CouponRoute = "/basket/coupon";

    public const string CheckoutRoute = "/basket/checkout";
}
