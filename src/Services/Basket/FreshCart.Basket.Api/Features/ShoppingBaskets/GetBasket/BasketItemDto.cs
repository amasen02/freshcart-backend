namespace FreshCart.Basket.Api.Features.ShoppingBaskets.GetBasket;

public sealed record BasketItemDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string PrimaryCategory,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountedUnitPrice,
    decimal LineTotal,
    string? ImageUrl,
    bool IsDigital);
