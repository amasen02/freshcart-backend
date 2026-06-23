namespace FreshCart.Pricing.Grpc.Models;

public sealed class DiscountRule
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal DiscountPercentage { get; set; }

    public DateTimeOffset ValidFromUtc { get; set; }

    public DateTimeOffset ValidToUtc { get; set; }

    public bool IsActive { get; set; }
}
