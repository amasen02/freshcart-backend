using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.AddItem;

public sealed record AddItemCommand(Guid CustomerId, Guid ProductId, int Quantity) : ICommand;
