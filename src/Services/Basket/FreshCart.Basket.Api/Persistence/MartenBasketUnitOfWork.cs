using FreshCart.Basket.Api.Domain;
using FreshCart.BuildingBlocks.Messaging.Outbox;
using Marten;

namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Marten-backed checkout commit: all three operations ride a single
/// <see cref="IDocumentSession.SaveChangesAsync"/>, which Marten wraps in one PostgreSQL transaction.
/// </summary>
public sealed class MartenBasketUnitOfWork(IDocumentSession documentSession) : IBasketUnitOfWork
{
    public Task CommitCheckoutAsync(
        ArchivedBasket archivedBasket,
        OutboxMessage checkoutOutboxMessage,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(archivedBasket);
        ArgumentNullException.ThrowIfNull(checkoutOutboxMessage);

        documentSession.Store(archivedBasket);
        documentSession.Store(checkoutOutboxMessage);
        documentSession.Delete<ShoppingBasket>(customerId);

        return documentSession.SaveChangesAsync(cancellationToken);
    }
}
