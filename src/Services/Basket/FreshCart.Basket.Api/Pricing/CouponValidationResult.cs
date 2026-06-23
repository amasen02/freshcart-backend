namespace FreshCart.Basket.Api.Pricing;

public sealed record CouponValidationResult(
    bool IsValid,
    string? ErrorMessage,
    decimal DiscountValue,
    string? DiscountType);
