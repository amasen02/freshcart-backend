using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Persistence;
using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using MediatR;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.RemoveItem;

public sealed class RemoveItemCommandHandler(
    IBasketRepository basketRepository,
    TimeProvider timeProvider)
    : ICommandHandler<RemoveItemCommand>
{
    public async Task<Unit> Handle(RemoveItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await basketRepository
            .MutateAsync(command.CustomerId, existingBasket => RemoveLine(existingBasket, command), cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }

    private ShoppingBasket RemoveLine(ShoppingBasket? existingBasket, RemoveItemCommand command)
    {
        var customerBasket = existingBasket ?? throw new NotFoundException("Basket", command.CustomerId);

        if (!customerBasket.RemoveItem(command.ProductId))
        {
            throw new NotFoundException("Basket item", command.ProductId);
        }

        customerBasket.UpdatedOnUtc = timeProvider.GetUtcNow();
        return customerBasket;
    }
}
