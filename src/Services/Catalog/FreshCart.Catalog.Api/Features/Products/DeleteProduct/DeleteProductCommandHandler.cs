using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Catalog.Api.Caching;
using FreshCart.Catalog.Api.Models;
using Marten;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace FreshCart.Catalog.Api.Features.Products.DeleteProduct;

/// <summary>
/// Soft delete: the document survives with <c>IsActive = false</c> because existing baskets and
/// orders still resolve the product's display data through this service.
/// </summary>
public sealed class DeleteProductCommandHandler(
    IDocumentSession documentSession,
    HybridCache hybridCache,
    TimeProvider timeProvider)
    : ICommandHandler<DeleteProductCommand>
{
    public async Task<Unit> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await documentSession.LoadAsync<Product>(command.ProductId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Product), command.ProductId);

        product.IsActive = false;
        product.UpdatedOnUtc = timeProvider.GetUtcNow();

        documentSession.Store(product);
        await documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await hybridCache.RemoveAsync(CatalogCachePolicy.ProductKey(product.Id), cancellationToken).ConfigureAwait(false);
        await hybridCache.RemoveAsync(CatalogCachePolicy.ProductKey(product.Slug), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
