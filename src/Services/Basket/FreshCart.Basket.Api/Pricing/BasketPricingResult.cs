namespace FreshCart.Basket.Api.Pricing;

public sealed record BasketPricingResult(
    IReadOnlyList<PricedBasketLine> Lines,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string? AppliedCoupon);
