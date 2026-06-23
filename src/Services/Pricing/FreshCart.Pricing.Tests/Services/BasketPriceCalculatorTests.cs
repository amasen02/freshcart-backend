using FluentAssertions;
using FreshCart.Pricing.Grpc.Models;
using FreshCart.Pricing.Grpc.Persistence;
using FreshCart.Pricing.Grpc.Services;
using FreshCart.Pricing.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace FreshCart.Pricing.Tests.Services;

/// <summary>
/// Exercises the full pricing pipeline against a real SQLite in-memory database so the discount
/// rule query, coupon validation and rounding behave exactly as they will in production.
/// </summary>
public sealed class BasketPriceCalculatorTests : IDisposable
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.Parse("5f0c2c5e-9a51-4f8e-b7d3-2c1a7e6b4d90");

    private readonly SqliteConnection _sqliteConnection;
    private readonly PricingDbContext _pricingDbContext;
    private readonly BasketPriceCalculator _calculator;

    public BasketPriceCalculatorTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var dbContextOptions = new DbContextOptionsBuilder<PricingDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _pricingDbContext = new PricingDbContext(dbContextOptions);
        _pricingDbContext.Database.EnsureCreated();

        var fixedClock = new FixedTimeProvider(FixedUtcNow);
        _calculator = new BasketPriceCalculator(
            _pricingDbContext,
            new CouponValidator(_pricingDbContext, fixedClock),
            Options.Create(new PricingOptions()),
            fixedClock);
    }

    [Fact]
    public async Task PlainTotalsAreReturnedWhenNoRulesOrCouponApply()
    {
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var request = MakeRequest(
            couponCode: null,
            new BasketPriceLine(firstProductId, "SKU-APPLES", 3.50m, 2),
            new BasketPriceLine(secondProductId, "SKU-MILK", 1.25m, 4));

        var result = await _calculator.CalculateAsync(request, CancellationToken.None);

        result.Subtotal.Should().Be(12.00m);
        result.DiscountTotal.Should().Be(0m);
        result.TaxTotal.Should().Be(0.96m);
        result.GrandTotal.Should().Be(12.96m);
        result.AppliedCouponCode.Should().BeNull();

        result.Lines.Should().HaveCount(2);
        result.Lines[0].Should().Be(new PricedBasketLine(firstProductId, 3.50m, 3.50m, 7.00m));
        result.Lines[1].Should().Be(new PricedBasketLine(secondProductId, 1.25m, 1.25m, 5.00m));
    }

    [Fact]
    public async Task HighestPercentageWinsWhenActiveRulesOverlap()
    {
        var productId = Guid.NewGuid();
        SeedDiscountRule(productId, "Spring promo 10%", 10m);
        SeedDiscountRule(productId, "Launch promo 25%", 25m);
        SeedDiscountRule(productId, "Disabled promo 90%", 90m, isActive: false);
        SeedDiscountRule(
            productId,
            "Finished promo 90%",
            90m,
            validFromUtc: FixedUtcNow.AddDays(-20),
            validToUtc: FixedUtcNow.AddDays(-10));

        var request = MakeRequest(couponCode: null, new BasketPriceLine(productId, "SKU-CHEESE", 10.00m, 1));

        var result = await _calculator.CalculateAsync(request, CancellationToken.None);

        result.Lines.Single().DiscountedUnitPrice.Should().Be(7.50m);
        result.Subtotal.Should().Be(10.00m);
        result.DiscountTotal.Should().Be(2.50m);
        result.TaxTotal.Should().Be(0.60m);
        result.GrandTotal.Should().Be(8.10m);
    }

    [Fact]
    public async Task PercentageCouponAppliesToTheDiscountedSubtotal()
    {
        var productId = Guid.NewGuid();
        SeedDiscountRule(productId, "Hamper deal 20%", 20m);
        SeedCoupon("SAVE10", CouponDiscountType.Percentage, 10m);

        var request = MakeRequest("SAVE10", new BasketPriceLine(productId, "SKU-HAMPER", 100.00m, 1));

        var result = await _calculator.CalculateAsync(request, CancellationToken.None);

        // 100 -> 80 after the line rule; the coupon takes 10% of 80, not of 100.
        result.Subtotal.Should().Be(100.00m);
        result.DiscountTotal.Should().Be(28.00m);
        result.TaxTotal.Should().Be(5.76m);
        result.GrandTotal.Should().Be(77.76m);
        result.AppliedCouponCode.Should().Be("SAVE10");
    }

    [Fact]
    public async Task FixedAmountCouponIsClampedSoGrandTotalNeverGoesBelowZero()
    {
        var productId = Guid.NewGuid();
        SeedCoupon("BIGSAVE", CouponDiscountType.FixedAmount, 50m);

        var request = MakeRequest("BIGSAVE", new BasketPriceLine(productId, "SKU-GUM", 4.00m, 1));

        var result = await _calculator.CalculateAsync(request, CancellationToken.None);

        result.Subtotal.Should().Be(4.00m);
        result.DiscountTotal.Should().Be(4.00m);
        result.TaxTotal.Should().Be(0m);
        result.GrandTotal.Should().Be(0m);
        result.AppliedCouponCode.Should().Be("BIGSAVE");
    }

    [Fact]
    public async Task CouponBelowItsMinimumOrderAmountIsNotApplied()
    {
        var productId = Guid.NewGuid();
        SeedCoupon("MIN20", CouponDiscountType.Percentage, 10m, minimumOrderAmount: 20m);

        var request = MakeRequest("MIN20", new BasketPriceLine(productId, "SKU-BREAD", 10.00m, 1));

        var result = await _calculator.CalculateAsync(request, CancellationToken.None);

        result.AppliedCouponCode.Should().BeNull();
        result.DiscountTotal.Should().Be(0m);
        result.TaxTotal.Should().Be(0.80m);
        result.GrandTotal.Should().Be(10.80m);
    }

    [Fact]
    public async Task TaxIsComputedOnTheDiscountedAmountNotTheOriginalSubtotal()
    {
        var productId = Guid.NewGuid();
        SeedDiscountRule(productId, "Half price 50%", 50m);

        var request = MakeRequest(couponCode: null, new BasketPriceLine(productId, "SKU-WINE", 50.00m, 2));

        var result = await _calculator.CalculateAsync(request, CancellationToken.None);

        result.Subtotal.Should().Be(100.00m);
        result.DiscountTotal.Should().Be(50.00m);
        result.TaxTotal.Should().Be(4.00m);
        result.GrandTotal.Should().Be(54.00m);
    }

    [Fact]
    public async Task MidpointFiguresRoundToTheNearestEvenCent()
    {
        var productId = Guid.NewGuid();
        SeedDiscountRule(productId, "Half price 50%", 50m);

        var request = MakeRequest(couponCode: null, new BasketPriceLine(productId, "SKU-SWEET", 0.17m, 1));

        var result = await _calculator.CalculateAsync(request, CancellationToken.None);

        // Raw discounted unit price is 0.085; banker's rounding lands on the even cent 0.08
        // where away-from-zero rounding would produce 0.09.
        result.Lines.Single().DiscountedUnitPrice.Should().Be(0.08m);
        result.Lines.Single().LineTotal.Should().Be(0.08m);
        result.Subtotal.Should().Be(0.17m);
        result.DiscountTotal.Should().Be(0.08m);
        result.TaxTotal.Should().Be(0.01m);
        result.GrandTotal.Should().Be(0.09m);
    }

    public void Dispose()
    {
        _pricingDbContext.Dispose();
        _sqliteConnection.Dispose();
    }

    private static BasketPricingRequest MakeRequest(string? couponCode, params BasketPriceLine[] lines) =>
        new(CustomerId, couponCode, "USD", lines);

    private void SeedDiscountRule(
        Guid productId,
        string name,
        decimal discountPercentage,
        bool isActive = true,
        DateTimeOffset? validFromUtc = null,
        DateTimeOffset? validToUtc = null)
    {
        _pricingDbContext.DiscountRules.Add(new DiscountRule
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Name = name,
            DiscountPercentage = discountPercentage,
            ValidFromUtc = validFromUtc ?? FixedUtcNow.AddDays(-1),
            ValidToUtc = validToUtc ?? FixedUtcNow.AddDays(30),
            IsActive = isActive,
        });
        _pricingDbContext.SaveChanges();
    }

    private void SeedCoupon(
        string code,
        CouponDiscountType discountType,
        decimal discountValue,
        decimal? minimumOrderAmount = null)
    {
        _pricingDbContext.CouponCodes.Add(new CouponCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            DiscountType = discountType,
            DiscountValue = discountValue,
            MinimumOrderAmount = minimumOrderAmount,
            ValidFromUtc = FixedUtcNow.AddDays(-1),
            ValidToUtc = FixedUtcNow.AddDays(30),
            IsActive = true,
        });
        _pricingDbContext.SaveChanges();
    }
}
