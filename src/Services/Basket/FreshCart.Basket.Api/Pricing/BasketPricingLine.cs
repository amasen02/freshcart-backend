namespace FreshCart.Basket.Api.Pricing;

public sealed record BasketPricingLine(Guid ProductId, string ProductSku, decimal UnitPrice, int Quantity);
