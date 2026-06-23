using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.RemoveCoupon;

public sealed record RemoveCouponCommand(Guid CustomerId) : ICommand;
