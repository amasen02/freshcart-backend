using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.GetBasket;

public sealed record GetBasketQuery(Guid CustomerId) : IQuery<BasketDto>;
