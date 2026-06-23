using FreshCart.Basket.Api.Domain;
using Marten;

namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Durable store for basket documents on Marten/PostgreSQL. Each method is its own unit of work;
/// checkout, which must commit several writes atomically, goes through <see cref="IBasketUnitOfWork"/>
/// instead.
/// </summary>
public sealed class MartenBasketRepository(IDocumentSession documentSession) : IBasketRepository
{
    public Task<ShoppingBasket?> GetAsync(Guid customerId, CancellationToken cancellationToken) =>
        documentSession.LoadAsync<ShoppingBasket>(customerId, cancellationToken);

    public Task UpsertAsync(ShoppingBasket basket, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(basket);

        documentSession.Store(basket);
        return documentSession.SaveChangesAsync(cancellationToken);
    }

    public Task MutateAsync(
        Guid customerId,
        Func<ShoppingBasket?, ShoppingBasket?> mutate,
        CancellationToken cancellationToken) =>
        MartenConcurrencyRetry.ExecuteAsync(documentSession, customerId, mutate, cancellationToken);

    public Task DeleteAsync(Guid customerId, CancellationToken cancellationToken)
    {
        documentSession.Delete<ShoppingBasket>(customerId);
        return documentSession.SaveChangesAsync(cancellationToken);
    }

    public Task ArchiveAsync(ArchivedBasket archivedBasket, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(archivedBasket);

        documentSession.Store(archivedBasket);
        return documentSession.SaveChangesAsync(cancellationToken);
    }
}
