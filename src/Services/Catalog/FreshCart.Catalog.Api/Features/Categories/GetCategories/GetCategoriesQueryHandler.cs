using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Catalog.Api.Caching;
using FreshCart.Catalog.Api.Data;
using Microsoft.Extensions.Caching.Hybrid;

namespace FreshCart.Catalog.Api.Features.Categories.GetCategories;

/// <summary>
/// Serves the nested storefront category tree from HybridCache for ten minutes. The handler itself
/// is the cache factory state so the lambda stays allocation-free and substitutable in tests.
/// </summary>
public sealed class GetCategoriesQueryHandler(
    ICatalogQueries catalogQueries,
    HybridCache hybridCache)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryNodeDto>>
{
    public async Task<IReadOnlyList<CategoryNodeDto>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await hybridCache.GetOrCreateAsync(
            CatalogCachePolicy.CategoryTreeKey,
            this,
            static async (handler, factoryCancellationToken) =>
                await handler.LoadCategoryTreeAsync(factoryCancellationToken).ConfigureAwait(false),
            CatalogCachePolicy.CategoryTreeEntryOptions,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<CategoryNodeDto>> LoadCategoryTreeAsync(CancellationToken cancellationToken)
    {
        var activeCategories = await catalogQueries.GetActiveCategoriesAsync(cancellationToken).ConfigureAwait(false);
        return CategoryTreeBuilder.Build(activeCategories);
    }
}
