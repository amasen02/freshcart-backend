namespace FreshCart.Pricing.Grpc.Services;

public sealed class PricingOptions
{
    public const string SectionName = "Pricing";
    public const decimal DefaultTaxRatePercentage = 8.0m;

    public decimal TaxRatePercentage { get; set; } = DefaultTaxRatePercentage;
}
