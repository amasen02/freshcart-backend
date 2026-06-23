using FreshCart.Basket.Api.Domain;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.GetBasket;

public sealed record BasketDto(
    Guid CustomerId,
    string CurrencyCode,
    IReadOnlyList<BasketItemDto> Items,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string? AppliedCoupon)
{
    public static BasketDto EmptyFor(Guid customerId) => new(
        customerId,
        BasketDefaults.CurrencyCode,
        [],
        Subtotal: decimal.Zero,
        DiscountTotal: decimal.Zero,
        TaxTotal: decimal.Zero,
        GrandTotal: decimal.Zero,
        AppliedCoupon: null);
}
