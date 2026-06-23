using FluentAssertions;
using FreshCart.Pricing.Grpc.Models;
using FreshCart.Pricing.Grpc.Persistence;
using FreshCart.Pricing.Grpc.Services;
using FreshCart.Pricing.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FreshCart.Pricing.Tests.Services;

public sealed class CouponValidatorTests : IDisposable
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _sqliteConnection;
    private readonly PricingDbContext _pricingDbContext;
    private readonly CouponValidator _couponValidator;

    public CouponValidatorTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var dbContextOptions = new DbContextOptionsBuilder<PricingDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _pricingDbContext = new PricingDbContext(dbContextOptions);
        _pricingDbContext.Database.EnsureCreated();

        _couponValidator = new CouponValidator(_pricingDbContext, new FixedTimeProvider(FixedUtcNow));
    }

    [Fact]
    public async Task UnknownCouponCodeIsInvalid()
    {
        var result = await _couponValidator.ValidateAsync("NOSUCHCODE", 100m, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Coupon 'NOSUCHCODE' does not exist.");
    }

    [Fact]
    public async Task ExpiredCouponIsInvalid()
    {
        SeedCoupon("OLDDEAL", validFromUtc: FixedUtcNow.AddDays(-30), validToUtc: FixedUtcNow.AddDays(-1));

        var result = await _couponValidator.ValidateAsync("OLDDEAL", 100m, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Coupon 'OLDDEAL' is expired or inactive.");
    }

    [Fact]
    public async Task InactiveCouponIsInvalid()
    {
        SeedCoupon("PAUSED", isActive: false);

        var result = await _couponValidator.ValidateAsync("PAUSED", 100m, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Coupon 'PAUSED' is expired or inactive.");
    }

    [Fact]
    public async Task CouponAtItsUsageLimitIsInvalid()
    {
        SeedCoupon("CROWDED", usageLimit: 5, usageCount: 5);

        var result = await _couponValidator.ValidateAsync("CROWDED", 100m, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Coupon 'CROWDED' has reached its usage limit.");
    }

    [Fact]
    public async Task CouponBelowItsUsageLimitIsStillValid()
    {
        SeedCoupon("ALMOSTFULL", usageLimit: 5, usageCount: 4);

        var result = await _couponValidator.ValidateAsync("ALMOSTFULL", 100m, CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task OrderBelowTheMinimumAmountIsInvalid()
    {
        SeedCoupon("MIN50", minimumOrderAmount: 50m);

        var result = await _couponValidator.ValidateAsync("MIN50", 49.99m, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Coupon 'MIN50' requires a minimum order of 50.00.");
    }

    [Fact]
    public async Task ValidCouponReturnsItsTypeAndValue()
    {
        SeedCoupon("FRESH5", discountType: CouponDiscountType.FixedAmount, discountValue: 5m, minimumOrderAmount: 25m);

        var result = await _couponValidator.ValidateAsync("FRESH5", 30m, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.CouponCode.Should().Be("FRESH5");
        result.DiscountType.Should().Be(CouponDiscountType.FixedAmount);
        result.DiscountValue.Should().Be(5m);
    }

    [Fact]
    public async Task CouponCodeIsTrimmedAndUppercasedBeforeLookup()
    {
        SeedCoupon("WELCOME10");

        var result = await _couponValidator.ValidateAsync("  welcome10  ", 100m, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.CouponCode.Should().Be("WELCOME10");
    }

    public void Dispose()
    {
        _pricingDbContext.Dispose();
        _sqliteConnection.Dispose();
    }

    private void SeedCoupon(
        string code,
        CouponDiscountType discountType = CouponDiscountType.Percentage,
        decimal discountValue = 10m,
        decimal? minimumOrderAmount = null,
        int? usageLimit = null,
        int usageCount = 0,
        bool isActive = true,
        DateTimeOffset? validFromUtc = null,
        DateTimeOffset? validToUtc = null)
    {
        _pricingDbContext.CouponCodes.Add(new CouponCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            DiscountType = discountType,
            DiscountValue = discountValue,
            MinimumOrderAmount = minimumOrderAmount,
            UsageLimit = usageLimit,
            UsageCount = usageCount,
            ValidFromUtc = validFromUtc ?? FixedUtcNow.AddDays(-1),
            ValidToUtc = validToUtc ?? FixedUtcNow.AddDays(30),
            IsActive = isActive,
        });
        _pricingDbContext.SaveChanges();
    }
}
