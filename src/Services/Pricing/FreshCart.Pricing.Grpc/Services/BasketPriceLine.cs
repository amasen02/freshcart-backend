namespace FreshCart.Pricing.Grpc.Services;

public sealed record BasketPriceLine(
    Guid ProductId,
    string ProductSku,
    decimal UnitPrice,
    int Quantity);
