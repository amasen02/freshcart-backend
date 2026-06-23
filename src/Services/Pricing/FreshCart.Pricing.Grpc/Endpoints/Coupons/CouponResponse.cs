using FreshCart.Pricing.Grpc.Models;

namespace FreshCart.Pricing.Grpc.Endpoints.Coupons;

public sealed record CouponResponse(
    Guid Id,
    string Code,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    decimal? MinimumOrderAmount,
    int? UsageLimit,
    int UsageCount,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ValidToUtc,
    bool IsActive);
