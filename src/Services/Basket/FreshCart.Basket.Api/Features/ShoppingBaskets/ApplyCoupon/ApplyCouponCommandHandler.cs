using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Persistence;
using FreshCart.Basket.Api.Pricing;
using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using MediatR;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.ApplyCoupon;

/// <summary>
/// Asks Pricing to validate the coupon against the current basket subtotal before storing the code.
/// The stored code is revalidated again at checkout, so a coupon expiring in between cannot leak a
/// discount.
/// </summary>
public sealed class ApplyCouponCommandHandler(
    IBasketRepository basketRepository,
    IBasketPricingClient basketPricingClient,
    TimeProvider timeProvider)
    : ICommandHandler<ApplyCouponCommand>
{
    public async Task<Unit> Handle(ApplyCouponCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var customerBasket = await basketRepository.GetAsync(command.CustomerId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Basket", command.CustomerId);

        if (customerBasket.IsEmpty)
        {
            throw new BadRequestException("Cannot apply a coupon to an empty basket.");
        }

        var couponValidation = await basketPricingClient
            .ValidateCouponAsync(command.Code, command.CustomerId, customerBasket.StoredSubtotal, cancellationToken)
            .ConfigureAwait(false);

        if (!couponValidation.IsValid)
        {
            throw new BadRequestException(
                "The coupon code cannot be applied.",
                couponValidation.ErrorMessage ?? "The coupon code is not valid.");
        }

        await basketRepository
            .MutateAsync(command.CustomerId, existingBasket => StoreCoupon(existingBasket, command), cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }

    private ShoppingBasket StoreCoupon(ShoppingBasket? existingBasket, ApplyCouponCommand command)
    {
        var customerBasket = existingBasket ?? throw new NotFoundException("Basket", command.CustomerId);

        customerBasket.CouponCode = command.Code;
        customerBasket.UpdatedOnUtc = timeProvider.GetUtcNow();
        return customerBasket;
    }
}
