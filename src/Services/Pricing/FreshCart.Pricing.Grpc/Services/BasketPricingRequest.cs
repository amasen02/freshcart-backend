namespace FreshCart.Pricing.Grpc.Services;

public sealed record BasketPricingRequest(
    Guid CustomerId,
    string? CouponCode,
    string CurrencyCode,
    IReadOnlyList<BasketPriceLine> Lines);
