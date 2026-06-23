using FluentValidation.TestHelper;
using FreshCart.Pricing.Grpc.Endpoints.Coupons;
using FreshCart.Pricing.Grpc.Models;
using Xunit;

namespace FreshCart.Pricing.Tests.Endpoints;

public sealed class CreateCouponRequestValidatorTests
{
    private static readonly DateTimeOffset WindowStart = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowEnd = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly CreateCouponRequestValidator _validator = new();

    [Fact]
    public void WellFormedRequestPasses()
    {
        var result = _validator.TestValidate(MakeRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCodeFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { Code = string.Empty });

        result.ShouldHaveValidationErrorFor(request => request.Code);
    }

    [Fact]
    public void LowercaseCodeFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { Code = "welcome10" });

        result.ShouldHaveValidationErrorFor(request => request.Code);
    }

    [Fact]
    public void CodeLongerThanTheColumnLimitFails()
    {
        var overlongCode = new string('A', PricingFieldLengths.CouponCode + 1);

        var result = _validator.TestValidate(MakeRequest() with { Code = overlongCode });

        result.ShouldHaveValidationErrorFor(request => request.Code);
    }

    [Fact]
    public void ZeroDiscountValueFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { DiscountValue = 0m });

        result.ShouldHaveValidationErrorFor(request => request.DiscountValue);
    }

    [Fact]
    public void PercentageDiscountAboveOneHundredFails()
    {
        var result = _validator.TestValidate(MakeRequest() with
        {
            DiscountType = CouponDiscountType.Percentage,
            DiscountValue = 100.01m,
        });

        result.ShouldHaveValidationErrorFor(request => request.DiscountValue);
    }

    [Fact]
    public void FixedAmountDiscountAboveOneHundredPasses()
    {
        var result = _validator.TestValidate(MakeRequest() with
        {
            DiscountType = CouponDiscountType.FixedAmount,
            DiscountValue = 250m,
        });

        result.ShouldNotHaveValidationErrorFor(request => request.DiscountValue);
    }

    [Fact]
    public void UndefinedDiscountTypeFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { DiscountType = (CouponDiscountType)99 });

        result.ShouldHaveValidationErrorFor(request => request.DiscountType);
    }

    [Fact]
    public void NonPositiveMinimumOrderAmountFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { MinimumOrderAmount = 0m });

        result.ShouldHaveValidationErrorFor(request => request.MinimumOrderAmount);
    }

    [Fact]
    public void OmittedMinimumOrderAmountPasses()
    {
        var result = _validator.TestValidate(MakeRequest() with { MinimumOrderAmount = null });

        result.ShouldNotHaveValidationErrorFor(request => request.MinimumOrderAmount);
    }

    [Fact]
    public void NonPositiveUsageLimitFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { UsageLimit = 0 });

        result.ShouldHaveValidationErrorFor(request => request.UsageLimit);
    }

    [Fact]
    public void ValidityWindowEndingBeforeItStartsFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { ValidToUtc = WindowStart.AddDays(-1) });

        result.ShouldHaveValidationErrorFor(request => request.ValidToUtc);
    }

    private static CreateCouponRequest MakeRequest() =>
        new(
            Code: "WELCOME10",
            DiscountType: CouponDiscountType.Percentage,
            DiscountValue: 10m,
            MinimumOrderAmount: 20m,
            UsageLimit: 100,
            ValidFromUtc: WindowStart,
            ValidToUtc: WindowEnd,
            IsActive: true);
}
