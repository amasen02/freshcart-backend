namespace FreshCart.Basket.Api.Features.ShoppingBaskets.AddItem;

public sealed record AddItemRequest(Guid ProductId, int Quantity);
