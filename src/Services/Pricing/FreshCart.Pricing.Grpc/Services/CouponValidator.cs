using System.Globalization;
using FreshCart.Pricing.Grpc.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Pricing.Grpc.Services;

public sealed class CouponValidator(PricingDbContext pricingDbContext, TimeProvider timeProvider)
{
    public async Task<CouponValidationResult> ValidateAsync(
        string couponCode,
        decimal orderSubtotal,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(couponCode);

        var normalisedCouponCode = couponCode.Trim().ToUpperInvariant();

        var coupon = await pricingDbContext.CouponCodes
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Code == normalisedCouponCode, cancellationToken)
            .ConfigureAwait(false);

        if (coupon is null)
        {
            return CouponValidationResult.Invalid($"Coupon '{normalisedCouponCode}' does not exist.");
        }

        var utcNow = timeProvider.GetUtcNow();
        if (!coupon.IsActive || utcNow < coupon.ValidFromUtc || utcNow > coupon.ValidToUtc)
        {
            return CouponValidationResult.Invalid($"Coupon '{normalisedCouponCode}' is expired or inactive.");
        }

        if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
        {
            return CouponValidationResult.Invalid($"Coupon '{normalisedCouponCode}' has reached its usage limit.");
        }

        if (coupon.MinimumOrderAmount.HasValue && orderSubtotal < coupon.MinimumOrderAmount.Value)
        {
            return CouponValidationResult.Invalid(string.Create(
                CultureInfo.InvariantCulture,
                $"Coupon '{normalisedCouponCode}' requires a minimum order of {coupon.MinimumOrderAmount.Value:0.00}."));
        }

        return CouponValidationResult.Valid(normalisedCouponCode, coupon.DiscountType, coupon.DiscountValue);
    }
}
