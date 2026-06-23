using FreshCart.Basket.Api.Domain;
using FreshCart.BuildingBlocks.Messaging.Outbox;

namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Atomic commit port for checkout. Archiving the basket, enqueuing the checkout event in the
/// outbox and removing the live basket must succeed or fail together; anything less either loses
/// a paid-for order event or leaves the customer with a ghost basket.
/// </summary>
public interface IBasketUnitOfWork
{
    Task CommitCheckoutAsync(
        ArchivedBasket archivedBasket,
        OutboxMessage checkoutOutboxMessage,
        Guid customerId,
        CancellationToken cancellationToken);
}
