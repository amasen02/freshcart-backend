using FreshCart.Basket.Api.Catalog;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Persistence;
using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using MediatR;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.AddItem;

/// <summary>
/// Validates the product against Catalog, captures its display snapshot onto the line and merges
/// quantities when the product is already in the basket (capped at the per-line maximum).
/// </summary>
public sealed class AddItemCommandHandler(
    IBasketRepository basketRepository,
    ICatalogProductClient catalogProductClient,
    TimeProvider timeProvider)
    : ICommandHandler<AddItemCommand>
{
    public async Task<Unit> Handle(AddItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var catalogProduct = await catalogProductClient
            .GetProductAsync(command.ProductId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Product", command.ProductId);

        if (!catalogProduct.IsActive)
        {
            throw new BadRequestException($"Product \"{catalogProduct.Name}\" is not available for purchase.");
        }

        await basketRepository
            .MutateAsync(command.CustomerId, existingBasket => Merge(existingBasket, command, catalogProduct), cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }

    private ShoppingBasket Merge(ShoppingBasket? existingBasket, AddItemCommand command, CatalogProduct catalogProduct)
    {
        var customerBasket = existingBasket ?? ShoppingBasket.CreateForCustomer(command.CustomerId);

        customerBasket.AddOrMergeItem(new BasketItem
        {
            ProductId = catalogProduct.ProductId,
            ProductSku = catalogProduct.Sku,
            ProductName = catalogProduct.Name,
            PrimaryCategory = catalogProduct.PrimaryCategory,
            UnitPrice = catalogProduct.Price,
            Quantity = command.Quantity,
            ImageUrl = catalogProduct.ImageUrl,
            IsDigital = catalogProduct.IsDigital,
        });

        customerBasket.UpdatedOnUtc = timeProvider.GetUtcNow();
        return customerBasket;
    }
}
