using FluentValidation.TestHelper;
using FreshCart.Basket.Api.Features.ShoppingBaskets.ApplyCoupon;
using Xunit;

namespace FreshCart.Basket.Tests.Features.ApplyCoupon;

public sealed class ApplyCouponCommandValidatorTests
{
    private const int MaxCouponCodeLength = 64;

    private readonly ApplyCouponCommandValidator _validator = new();

    [Fact]
    public void WellFormedCommandPasses()
    {
        var result = _validator.TestValidate(new ApplyCouponCommand(Guid.NewGuid(), "FRESH10"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCustomerIdFails()
    {
        var result = _validator.TestValidate(new ApplyCouponCommand(Guid.Empty, "FRESH10"));

        result.ShouldHaveValidationErrorFor(command => command.CustomerId);
    }

    [Fact]
    public void EmptyCodeFails()
    {
        var result = _validator.TestValidate(new ApplyCouponCommand(Guid.NewGuid(), string.Empty));

        result.ShouldHaveValidationErrorFor(command => command.Code);
    }

    [Fact]
    public void OverlongCodeFails()
    {
        var result = _validator.TestValidate(
            new ApplyCouponCommand(Guid.NewGuid(), new string('X', MaxCouponCodeLength + 1)));

        result.ShouldHaveValidationErrorFor(command => command.Code);
    }
}
