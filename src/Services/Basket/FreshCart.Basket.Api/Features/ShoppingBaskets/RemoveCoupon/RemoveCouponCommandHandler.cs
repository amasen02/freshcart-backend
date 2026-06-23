using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Persistence;
using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using MediatR;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.RemoveCoupon;

/// <summary>
/// Clears the stored coupon code. Idempotent: removing a coupon that is not there is still success.
/// </summary>
public sealed class RemoveCouponCommandHandler(
    IBasketRepository basketRepository,
    TimeProvider timeProvider)
    : ICommandHandler<RemoveCouponCommand>
{
    public async Task<Unit> Handle(RemoveCouponCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await basketRepository
            .MutateAsync(command.CustomerId, existingBasket => ClearCoupon(existingBasket, command), cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }

    private ShoppingBasket? ClearCoupon(ShoppingBasket? existingBasket, RemoveCouponCommand command)
    {
        var customerBasket = existingBasket ?? throw new NotFoundException("Basket", command.CustomerId);

        if (customerBasket.CouponCode is null)
        {
            return null;
        }

        customerBasket.CouponCode = null;
        customerBasket.UpdatedOnUtc = timeProvider.GetUtcNow();
        return customerBasket;
    }
}
