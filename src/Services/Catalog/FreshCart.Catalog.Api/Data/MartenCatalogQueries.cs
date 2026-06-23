using FreshCart.Catalog.Api.Models;
using Marten;

namespace FreshCart.Catalog.Api.Data;

public sealed class MartenCatalogQueries(IQuerySession querySession) : ICatalogQueries
{
    public Task<bool> ProductSkuExistsAsync(string productSku, CancellationToken cancellationToken) =>
        querySession.Query<Product>()
            .Where(product => product.Sku == productSku)
            .AnyAsync(cancellationToken);

    public Task<bool> ProductSlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        querySession.Query<Product>()
            .Where(product => product.Slug == slug)
            .AnyAsync(cancellationToken);

    public Task<bool> CategorySlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        querySession.Query<Category>()
            .Where(category => category.Slug == slug)
            .AnyAsync(cancellationToken);

    public Task<bool> BrandSlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        querySession.Query<Brand>()
            .Where(brand => brand.Slug == slug)
            .AnyAsync(cancellationToken);

    public Task<bool> AnyCategoriesExistAsync(CancellationToken cancellationToken) =>
        querySession.Query<Category>().AnyAsync(cancellationToken);

    public Task<Product?> FindProductBySlugAsync(string slug, CancellationToken cancellationToken) =>
        querySession.Query<Product>()
            .Where(product => product.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken) =>
        querySession.Query<Category>()
            .Where(category => category.IsActive)
            .ToListAsync(cancellationToken);

    public Task<IReadOnlyList<Brand>> GetActiveBrandsAsync(CancellationToken cancellationToken) =>
        querySession.Query<Brand>()
            .Where(brand => brand.IsActive)
            .OrderBy(brand => brand.Name)
            .ToListAsync(cancellationToken);
}
