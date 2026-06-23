namespace FreshCart.Pricing.Grpc.Models;

public sealed class CouponCode
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public CouponDiscountType DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal? MinimumOrderAmount { get; set; }

    public int? UsageLimit { get; set; }

    public int UsageCount { get; set; }

    public DateTimeOffset ValidFromUtc { get; set; }

    public DateTimeOffset ValidToUtc { get; set; }

    public bool IsActive { get; set; }
}
