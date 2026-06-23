using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.RemoveItem;

public sealed record RemoveItemCommand(Guid CustomerId, Guid ProductId) : ICommand;
