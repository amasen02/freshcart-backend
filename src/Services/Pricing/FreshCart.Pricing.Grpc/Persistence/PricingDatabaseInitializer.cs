using FreshCart.Pricing.Grpc.Models;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Pricing.Grpc.Persistence;

/// <summary>
/// Creates the SQLite schema with <c>EnsureCreatedAsync</c>: a single-file embedded reference
/// store evolves by recreation, so the migrations engine would be dead weight here.
/// </summary>
public sealed class PricingDatabaseInitializer(
    IServiceScopeFactory serviceScopeFactory,
    IHostEnvironment hostEnvironment,
    TimeProvider timeProvider) : IHostedService
{
    private static readonly TimeSpan SeedCouponValidityWindow = TimeSpan.FromDays(365);
    private static readonly TimeSpan SeedCouponBackdate = TimeSpan.FromDays(1);
    private static readonly TimeSpan ExpiredSeedCouponAge = TimeSpan.FromDays(30);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using (serviceScope.ConfigureAwait(false))
        {
            var pricingDbContext = serviceScope.ServiceProvider.GetRequiredService<PricingDbContext>();

            await pricingDbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

            if (hostEnvironment.IsDevelopment())
            {
                await SeedDevelopmentCouponsAsync(pricingDbContext, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedDevelopmentCouponsAsync(PricingDbContext pricingDbContext, CancellationToken cancellationToken)
    {
        var anyCouponExists = await pricingDbContext.CouponCodes
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
        if (anyCouponExists)
        {
            return;
        }

        var utcNow = timeProvider.GetUtcNow();

        pricingDbContext.CouponCodes.AddRange(
            new CouponCode
            {
                Id = Guid.NewGuid(),
                Code = "WELCOME10",
                DiscountType = CouponDiscountType.Percentage,
                DiscountValue = 10m,
                MinimumOrderAmount = 20m,
                ValidFromUtc = utcNow - SeedCouponBackdate,
                ValidToUtc = utcNow + SeedCouponValidityWindow,
                IsActive = true,
            },
            new CouponCode
            {
                Id = Guid.NewGuid(),
                Code = "FRESH5",
                DiscountType = CouponDiscountType.FixedAmount,
                DiscountValue = 5m,
                MinimumOrderAmount = 25m,
                ValidFromUtc = utcNow - SeedCouponBackdate,
                ValidToUtc = utcNow + SeedCouponValidityWindow,
                IsActive = true,
            },
            new CouponCode
            {
                Id = Guid.NewGuid(),
                Code = "EXPIRED1",
                DiscountType = CouponDiscountType.Percentage,
                DiscountValue = 15m,
                ValidFromUtc = utcNow - ExpiredSeedCouponAge,
                ValidToUtc = utcNow - SeedCouponBackdate,
                IsActive = true,
            });

        await pricingDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
