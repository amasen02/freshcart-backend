using FreshCart.Basket.Api.Domain;
using Marten;

namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Marten implementation of the price refresh: one child-collection query discovers every affected
/// basket, then each is rewritten through its own optimistic-concurrency cycle. A basket whose lines
/// already carry the new price is skipped, so redelivered events write nothing, and a basket the
/// customer is editing at the same time is merged on retry rather than overwritten.
/// </summary>
public sealed class MartenBasketPriceRefresher(IDocumentSession documentSession, TimeProvider timeProvider) : IBasketPriceRefresher
{
    public async Task<IReadOnlyList<Guid>> RefreshUnitPriceAsync(
        Guid productId,
        decimal newUnitPrice,
        CancellationToken cancellationToken)
    {
        var affectedCustomerIds = await documentSession.Query<ShoppingBasket>()
            .Where(basket => basket.Items.Any(item => item.ProductId == productId))
            .Select(basket => basket.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var refreshedCustomerIds = new List<Guid>(affectedCustomerIds.Count);

        foreach (var customerId in affectedCustomerIds)
        {
            var wasRewritten = await MartenConcurrencyRetry
                .ExecuteAsync<ShoppingBasket>(
                    documentSession,
                    customerId,
                    basket => RewriteStaleLines(basket, productId, newUnitPrice),
                    cancellationToken)
                .ConfigureAwait(false);

            if (wasRewritten)
            {
                refreshedCustomerIds.Add(customerId);
            }
        }

        return refreshedCustomerIds;
    }

    private ShoppingBasket? RewriteStaleLines(ShoppingBasket? basket, Guid productId, decimal newUnitPrice)
    {
        if (basket is null)
        {
            return null;
        }

        var staleLines = basket.Items
            .Where(item => item.ProductId == productId && item.UnitPrice != newUnitPrice)
            .ToList();

        if (staleLines.Count == 0)
        {
            return null;
        }

        foreach (var staleLine in staleLines)
        {
            staleLine.UnitPrice = newUnitPrice;
        }

        basket.UpdatedOnUtc = timeProvider.GetUtcNow();
        return basket;
    }
}
