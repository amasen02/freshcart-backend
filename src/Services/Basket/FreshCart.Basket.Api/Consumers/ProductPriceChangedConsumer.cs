using FreshCart.Basket.Api.Persistence;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using MassTransit;

namespace FreshCart.Basket.Api.Consumers;

/// <summary>
/// Keeps the display snapshot on live basket lines aligned with Catalog price changes. Idempotent
/// by construction: the refresher sets lines to the new price and skips lines that already carry
/// it, so a redelivered event rewrites nothing and evicts nothing.
/// </summary>
public sealed partial class ProductPriceChangedConsumer(
    IBasketPriceRefresher basketPriceRefresher,
    IBasketCacheInvalidator basketCacheInvalidator,
    ILogger<ProductPriceChangedConsumer> logger) : IConsumer<ProductPriceChangedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductPriceChangedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var priceChange = context.Message;

        var refreshedCustomerIds = await basketPriceRefresher
            .RefreshUnitPriceAsync(priceChange.ProductId, priceChange.NewPrice, context.CancellationToken)
            .ConfigureAwait(false);

        foreach (var refreshedCustomerId in refreshedCustomerIds)
        {
            await basketCacheInvalidator
                .InvalidateAsync(refreshedCustomerId, context.CancellationToken)
                .ConfigureAwait(false);
        }

        LogBasketPricesRefreshed(priceChange.ProductId, priceChange.NewPrice, refreshedCustomerIds.Count);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Refreshed stored price for product {ProductId} to {NewPrice} across {RefreshedBasketCount} baskets")]
    private partial void LogBasketPricesRefreshed(Guid productId, decimal newPrice, int refreshedBasketCount);
}
