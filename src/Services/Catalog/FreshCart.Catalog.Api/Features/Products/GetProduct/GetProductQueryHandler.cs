using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Catalog.Api.Caching;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Models;
using Marten;
using Microsoft.Extensions.Caching.Hybrid;

namespace FreshCart.Catalog.Api.Features.Products.GetProduct;

/// <summary>
/// Single-product read through HybridCache. A miss throws <see cref="NotFoundException"/> from
/// inside the factory, so unknown identifiers are never cached and a product created moments later
/// is immediately visible.
/// </summary>
public sealed class GetProductQueryHandler(
    IQuerySession querySession,
    ICatalogQueries catalogQueries,
    HybridCache hybridCache)
    : IQueryHandler<GetProductQuery, ProductDetailsDto>
{
    public async Task<ProductDetailsDto> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cacheKey = Guid.TryParse(query.IdOrSlug, out var productId)
            ? CatalogCachePolicy.ProductKey(productId)
            : CatalogCachePolicy.ProductKey(query.IdOrSlug);

        return await hybridCache.GetOrCreateAsync(
            cacheKey,
            (Handler: this, query.IdOrSlug),
            static async (state, factoryCancellationToken) =>
                await state.Handler.LoadProductDetailsAsync(state.IdOrSlug, factoryCancellationToken).ConfigureAwait(false),
            CatalogCachePolicy.ProductEntryOptions,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProductDetailsDto> LoadProductDetailsAsync(string idOrSlug, CancellationToken cancellationToken)
    {
        var product = (Guid.TryParse(idOrSlug, out var productId)
            ? await querySession.LoadAsync<Product>(productId, cancellationToken).ConfigureAwait(false)
            : await catalogQueries.FindProductBySlugAsync(idOrSlug, cancellationToken).ConfigureAwait(false))
            ?? throw new NotFoundException(nameof(Product), idOrSlug);

        var category = await querySession.LoadAsync<Category>(product.CategoryId, cancellationToken).ConfigureAwait(false);
        var brand = await querySession.LoadAsync<Brand>(product.BrandId, cancellationToken).ConfigureAwait(false);

        return ProductDetailsDto.FromProduct(product, category?.Name ?? string.Empty, brand?.Name ?? string.Empty);
    }
}
