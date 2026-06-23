using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.UpdateItemQuantity;

public sealed record UpdateItemQuantityCommand(Guid CustomerId, Guid ProductId, int Quantity) : ICommand;
