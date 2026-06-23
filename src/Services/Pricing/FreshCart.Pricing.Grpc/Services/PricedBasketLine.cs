namespace FreshCart.Pricing.Grpc.Services;

public sealed record PricedBasketLine(
    Guid ProductId,
    decimal UnitPrice,
    decimal DiscountedUnitPrice,
    decimal LineTotal);
