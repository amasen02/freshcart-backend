using FreshCart.Basket.Api.Domain;

namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Persistence port for the basket documents. The caching decorator wraps this same contract, so
/// handlers never know whether a read came from Redis or PostgreSQL.
/// </summary>
public interface IBasketRepository
{
    Task<ShoppingBasket?> GetAsync(Guid customerId, CancellationToken cancellationToken);

    Task UpsertAsync(ShoppingBasket basket, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the customer's basket, applies <paramref name="mutate"/> to it (or to null when no basket
    /// exists yet) and persists the result under optimistic concurrency, retrying against a fresh
    /// snapshot if a concurrent write wins the version check. The mutation must be a pure in-memory
    /// change so re-running it on retry is safe; perform any external lookups before calling. Returning
    /// null from <paramref name="mutate"/> persists nothing.
    /// </summary>
    Task MutateAsync(
        Guid customerId,
        Func<ShoppingBasket?, ShoppingBasket?> mutate,
        CancellationToken cancellationToken);

    Task DeleteAsync(Guid customerId, CancellationToken cancellationToken);

    Task ArchiveAsync(ArchivedBasket archivedBasket, CancellationToken cancellationToken);
}
