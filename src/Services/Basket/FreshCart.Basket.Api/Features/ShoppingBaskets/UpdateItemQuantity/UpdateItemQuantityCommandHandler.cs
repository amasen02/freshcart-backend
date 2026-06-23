using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Persistence;
using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using MediatR;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.UpdateItemQuantity;

/// <summary>
/// Sets the quantity on an existing line; quantity zero removes the line entirely.
/// </summary>
public sealed class UpdateItemQuantityCommandHandler(
    IBasketRepository basketRepository,
    TimeProvider timeProvider)
    : ICommandHandler<UpdateItemQuantityCommand>
{
    public async Task<Unit> Handle(UpdateItemQuantityCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await basketRepository
            .MutateAsync(command.CustomerId, existingBasket => ApplyQuantity(existingBasket, command), cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }

    private ShoppingBasket ApplyQuantity(ShoppingBasket? existingBasket, UpdateItemQuantityCommand command)
    {
        var customerBasket = existingBasket ?? throw new NotFoundException("Basket", command.CustomerId);

        if (!customerBasket.SetItemQuantity(command.ProductId, command.Quantity))
        {
            throw new NotFoundException("Basket item", command.ProductId);
        }

        customerBasket.UpdatedOnUtc = timeProvider.GetUtcNow();
        return customerBasket;
    }
}
