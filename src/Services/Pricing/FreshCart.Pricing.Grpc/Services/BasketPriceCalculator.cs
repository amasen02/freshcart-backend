using FreshCart.Pricing.Grpc.Models;
using FreshCart.Pricing.Grpc.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FreshCart.Pricing.Grpc.Services;

public sealed class BasketPriceCalculator(
    PricingDbContext pricingDbContext,
    CouponValidator couponValidator,
    IOptions<PricingOptions> pricingOptions,
    TimeProvider timeProvider)
{
    private const decimal PercentageDivisor = 100m;

    public async Task<BasketPriceResult> CalculateAsync(BasketPricingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bestDiscountPercentageByProductId =
            await LoadBestDiscountPercentagesAsync(request.Lines, cancellationToken).ConfigureAwait(false);

        var pricedLines = new List<PricedBasketLine>(request.Lines.Count);
        var rawSubtotal = 0m;
        var rawDiscountedSubtotal = 0m;

        foreach (var line in request.Lines)
        {
            var discountPercentage = bestDiscountPercentageByProductId.GetValueOrDefault(line.ProductId);
            var rawDiscountedUnitPrice = line.UnitPrice * (1m - (discountPercentage / PercentageDivisor));
            var rawLineTotal = rawDiscountedUnitPrice * line.Quantity;

            rawSubtotal += line.UnitPrice * line.Quantity;
            rawDiscountedSubtotal += rawLineTotal;

            pricedLines.Add(new PricedBasketLine(
                line.ProductId,
                MoneyRounding.ToCurrency(line.UnitPrice),
                MoneyRounding.ToCurrency(rawDiscountedUnitPrice),
                MoneyRounding.ToCurrency(rawLineTotal)));
        }

        var (couponDiscount, appliedCouponCode) =
            await ApplyCouponAsync(request.CouponCode, rawDiscountedSubtotal, cancellationToken).ConfigureAwait(false);

        var rawTaxBase = rawDiscountedSubtotal - couponDiscount;
        var rawTaxTotal = rawTaxBase * pricingOptions.Value.TaxRatePercentage / PercentageDivisor;

        return new BasketPriceResult(
            pricedLines,
            MoneyRounding.ToCurrency(rawSubtotal),
            MoneyRounding.ToCurrency(rawSubtotal - rawDiscountedSubtotal + couponDiscount),
            MoneyRounding.ToCurrency(rawTaxTotal),
            MoneyRounding.ToCurrency(rawTaxBase + rawTaxTotal),
            appliedCouponCode);
    }

    private async Task<Dictionary<Guid, decimal>> LoadBestDiscountPercentagesAsync(
        IReadOnlyList<BasketPriceLine> lines,
        CancellationToken cancellationToken)
    {
        var productIds = lines.Select(line => line.ProductId).Distinct().ToArray();

        // One round-trip on the (ProductId, IsActive) index for the whole basket; the validity
        // window and the highest-percentage pick run in memory because SQLite compares
        // DateTimeOffset and decimal columns as TEXT, making those SQL translations unreliable.
        var candidateRules = await pricingDbContext.DiscountRules
            .AsNoTracking()
            .Where(rule => rule.IsActive && productIds.Contains(rule.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var utcNow = timeProvider.GetUtcNow();

        return candidateRules
            .Where(rule => rule.ValidFromUtc <= utcNow && rule.ValidToUtc >= utcNow)
            .GroupBy(rule => rule.ProductId)
            .ToDictionary(rulesForProduct => rulesForProduct.Key, rulesForProduct => rulesForProduct.Max(rule => rule.DiscountPercentage));
    }

    private async Task<(decimal CouponDiscount, string? AppliedCouponCode)> ApplyCouponAsync(
        string? couponCode,
        decimal discountedSubtotal,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return (0m, null);
        }

        var couponValidation = await couponValidator
            .ValidateAsync(couponCode, discountedSubtotal, cancellationToken)
            .ConfigureAwait(false);

        if (!couponValidation.IsValid)
        {
            return (0m, null);
        }

        // Fixed-amount coupons are clamped to the discounted subtotal so the grand total can never
        // go below zero; the tax base inherits the same floor because tax is computed after it.
        var couponDiscount = couponValidation.DiscountType == CouponDiscountType.Percentage
            ? discountedSubtotal * couponValidation.DiscountValue / PercentageDivisor
            : Math.Min(couponValidation.DiscountValue, discountedSubtotal);

        return (couponDiscount, couponValidation.CouponCode);
    }
}
