using FreshCart.Pricing.Grpc.Models;

namespace FreshCart.Pricing.Grpc.Endpoints.Coupons;

public sealed record CreateCouponRequest(
    string Code,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    decimal? MinimumOrderAmount,
    int? UsageLimit,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ValidToUtc,
    bool IsActive);
