using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Catalog.Api.Caching;
using FreshCart.Catalog.Api.Models;
using Marten;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace FreshCart.Catalog.Api.Features.Products.UpdateProduct;

/// <summary>
/// Full replace of the editable product fields. The slug stays fixed after creation so storefront
/// URLs survive renames, and the sku stays fixed because Inventory and Basket key on it.
/// </summary>
public sealed class UpdateProductCommandHandler(
    IDocumentSession documentSession,
    IPublishEndpoint publishEndpoint,
    HybridCache hybridCache,
    TimeProvider timeProvider)
    : ICommandHandler<UpdateProductCommand>
{
    public async Task<Unit> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await documentSession.LoadAsync<Product>(command.ProductId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Product), command.ProductId);

        _ = await documentSession.LoadAsync<Category>(command.CategoryId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Category), command.CategoryId);

        _ = await documentSession.LoadAsync<Brand>(command.BrandId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Brand), command.BrandId);

        var previousBasePrice = product.BasePrice;

        product.Name = command.Name.Trim();
        product.Description = command.Description;
        product.BasePrice = command.BasePrice;
        product.CurrencyCode = command.CurrencyCode;
        product.CategoryId = command.CategoryId;
        product.BrandId = command.BrandId;
        product.IsDigital = command.IsDigital;
        product.IsActive = command.IsActive;
        product.Images = [.. command.Images ?? []];
        product.Attributes = [.. command.Attributes ?? []];
        product.UpdatedOnUtc = timeProvider.GetUtcNow();

        documentSession.Store(product);
        await documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (previousBasePrice != command.BasePrice)
        {
            await publishEndpoint.Publish(
                new ProductPriceChangedIntegrationEvent
                {
                    ProductId = product.Id,
                    ProductSku = product.Sku,
                    OldPrice = previousBasePrice,
                    NewPrice = product.BasePrice,
                },
                cancellationToken).ConfigureAwait(false);
        }

        await hybridCache.RemoveAsync(CatalogCachePolicy.ProductKey(product.Id), cancellationToken).ConfigureAwait(false);
        await hybridCache.RemoveAsync(CatalogCachePolicy.ProductKey(product.Slug), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
