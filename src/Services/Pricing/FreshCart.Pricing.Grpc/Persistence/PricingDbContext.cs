using FreshCart.Pricing.Grpc.Models;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Pricing.Grpc.Persistence;

public sealed class PricingDbContext(DbContextOptions<PricingDbContext> options) : DbContext(options)
{
    private const int DiscountPercentagePrecision = 5;
    private const int MoneyPrecision = 10;
    private const int MoneyScale = 2;

    public DbSet<DiscountRule> DiscountRules => Set<DiscountRule>();

    public DbSet<CouponCode> CouponCodes => Set<CouponCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiscountRule>(discountRuleBuilder =>
        {
            discountRuleBuilder.HasKey(discountRule => discountRule.Id);
            discountRuleBuilder.Property(discountRule => discountRule.Name)
                .HasMaxLength(PricingFieldLengths.DiscountRuleName)
                .IsRequired();
            discountRuleBuilder.Property(discountRule => discountRule.DiscountPercentage)
                .HasPrecision(DiscountPercentagePrecision, MoneyScale);
            discountRuleBuilder.HasIndex(discountRule => new { discountRule.ProductId, discountRule.IsActive });
        });

        modelBuilder.Entity<CouponCode>(couponCodeBuilder =>
        {
            couponCodeBuilder.HasKey(couponCode => couponCode.Id);
            couponCodeBuilder.Property(couponCode => couponCode.Code)
                .HasMaxLength(PricingFieldLengths.CouponCode)
                .IsRequired();
            couponCodeBuilder.HasIndex(couponCode => couponCode.Code).IsUnique();
            couponCodeBuilder.Property(couponCode => couponCode.DiscountType)
                .HasConversion<string>()
                .HasMaxLength(PricingFieldLengths.CouponDiscountType);
            couponCodeBuilder.Property(couponCode => couponCode.DiscountValue)
                .HasPrecision(MoneyPrecision, MoneyScale);
            couponCodeBuilder.Property(couponCode => couponCode.MinimumOrderAmount)
                .HasPrecision(MoneyPrecision, MoneyScale);
        });
    }
}
