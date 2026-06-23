using FreshCart.Pricing.Grpc.Models;

namespace FreshCart.Pricing.Grpc.Services;

/// <summary>
/// Outcome of a coupon check. Factory-only construction guarantees a valid result always carries
/// the normalised code, type and value, and an invalid result always carries the reason.
/// </summary>
public sealed record CouponValidationResult
{
    private CouponValidationResult()
    {
    }

    public bool IsValid { get; private init; }

    public string? ErrorMessage { get; private init; }

    public string? CouponCode { get; private init; }

    public CouponDiscountType DiscountType { get; private init; }

    public decimal DiscountValue { get; private init; }

    public static CouponValidationResult Valid(string couponCode, CouponDiscountType discountType, decimal discountValue) =>
        new()
        {
            IsValid = true,
            CouponCode = couponCode,
            DiscountType = discountType,
            DiscountValue = discountValue,
        };

    public static CouponValidationResult Invalid(string errorMessage) =>
        new()
        {
            IsValid = false,
            ErrorMessage = errorMessage,
        };
}
