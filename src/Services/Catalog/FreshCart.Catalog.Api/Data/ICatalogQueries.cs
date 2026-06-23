using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Api.Data;

/// <summary>
/// Narrow read port for the non-identifier lookups the handlers need. Identifier loads go through
/// <c>IQuerySession.LoadAsync</c> directly; these members exist because Marten's LINQ operators are
/// static extensions that cannot be substituted in unit tests.
/// </summary>
public interface ICatalogQueries
{
    Task<bool> ProductSkuExistsAsync(string productSku, CancellationToken cancellationToken);

    Task<bool> ProductSlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task<bool> CategorySlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task<bool> BrandSlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task<bool> AnyCategoriesExistAsync(CancellationToken cancellationToken);

    Task<Product?> FindProductBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>Active brands ordered by name; the ordering is part of the contract.</summary>
    Task<IReadOnlyList<Brand>> GetActiveBrandsAsync(CancellationToken cancellationToken);
}
