using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.ApplyCoupon;

public sealed record ApplyCouponCommand(Guid CustomerId, string Code) : ICommand;
