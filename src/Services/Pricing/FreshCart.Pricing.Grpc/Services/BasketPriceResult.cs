namespace FreshCart.Pricing.Grpc.Services;

public sealed record BasketPriceResult(
    IReadOnlyList<PricedBasketLine> Lines,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string? AppliedCouponCode);
