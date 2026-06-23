using FluentValidation.TestHelper;
using FreshCart.Pricing.Grpc.Endpoints.DiscountRules;
using FreshCart.Pricing.Grpc.Models;
using Xunit;

namespace FreshCart.Pricing.Tests.Endpoints;

public sealed class CreateDiscountRuleRequestValidatorTests
{
    private static readonly DateTimeOffset WindowStart = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowEnd = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly CreateDiscountRuleRequestValidator _validator = new();

    [Fact]
    public void WellFormedRequestPasses()
    {
        var result = _validator.TestValidate(MakeRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyProductIdFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { ProductId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(request => request.ProductId);
    }

    [Fact]
    public void EmptyNameFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { Name = string.Empty });

        result.ShouldHaveValidationErrorFor(request => request.Name);
    }

    [Fact]
    public void NameLongerThanTheColumnLimitFails()
    {
        var overlongName = new string('a', PricingFieldLengths.DiscountRuleName + 1);

        var result = _validator.TestValidate(MakeRequest() with { Name = overlongName });

        result.ShouldHaveValidationErrorFor(request => request.Name);
    }

    [Fact]
    public void ZeroPercentageFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { DiscountPercentage = 0m });

        result.ShouldHaveValidationErrorFor(request => request.DiscountPercentage);
    }

    [Fact]
    public void PercentageAboveOneHundredFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { DiscountPercentage = 100.01m });

        result.ShouldHaveValidationErrorFor(request => request.DiscountPercentage);
    }

    [Fact]
    public void ValidityWindowEndingBeforeItStartsFails()
    {
        var result = _validator.TestValidate(MakeRequest() with { ValidToUtc = WindowStart.AddDays(-1) });

        result.ShouldHaveValidationErrorFor(request => request.ValidToUtc);
    }

    private static CreateDiscountRuleRequest MakeRequest() =>
        new(
            ProductId: Guid.NewGuid(),
            Name: "Summer launch 15%",
            DiscountPercentage: 15m,
            ValidFromUtc: WindowStart,
            ValidToUtc: WindowEnd,
            IsActive: true);
}
