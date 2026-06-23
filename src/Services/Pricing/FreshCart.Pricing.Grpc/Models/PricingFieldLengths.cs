namespace FreshCart.Pricing.Grpc.Models;

/// <summary>
/// Column lengths shared by the EF Core model configuration and the request validators so the
/// two can never drift apart.
/// </summary>
public static class PricingFieldLengths
{
    public const int DiscountRuleName = 128;
    public const int CouponCode = 32;
    public const int CouponDiscountType = 16;
}
