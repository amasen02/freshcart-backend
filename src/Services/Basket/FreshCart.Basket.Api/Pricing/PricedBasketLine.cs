namespace FreshCart.Basket.Api.Pricing;

public sealed record PricedBasketLine(Guid ProductId, decimal UnitPrice, decimal DiscountedUnitPrice, decimal LineTotal);
