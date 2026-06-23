using FreshCart.Basket.Api.Consumers;
using FreshCart.Basket.Api.Persistence;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FreshCart.Basket.Tests.Consumers;

public sealed class ProductPriceChangedConsumerTests
{
    private readonly IBasketPriceRefresher _priceRefresher = Substitute.For<IBasketPriceRefresher>();
    private readonly IBasketCacheInvalidator _cacheInvalidator = Substitute.For<IBasketCacheInvalidator>();
    private readonly ProductPriceChangedConsumer _consumer;

    public ProductPriceChangedConsumerTests()
    {
        _consumer = new ProductPriceChangedConsumer(
            _priceRefresher,
            _cacheInvalidator,
            NullLogger<ProductPriceChangedConsumer>.Instance);
    }

    [Fact]
    public async Task EveryRefreshedBasketGetsItsCacheEntryEvicted()
    {
        var priceChange = PriceChangeFor(Guid.NewGuid(), newPrice: 3.20m);
        var firstCustomerId = Guid.NewGuid();
        var secondCustomerId = Guid.NewGuid();
        _priceRefresher.RefreshUnitPriceAsync(priceChange.ProductId, 3.20m, Arg.Any<CancellationToken>())
            .Returns([firstCustomerId, secondCustomerId]);

        await _consumer.Consume(ConsumeContextFor(priceChange));

        await _cacheInvalidator.Received(1).InvalidateAsync(firstCustomerId, Arg.Any<CancellationToken>());
        await _cacheInvalidator.Received(1).InvalidateAsync(secondCustomerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RedeliveredEventThatChangesNoLinesEvictsNoCacheEntries()
    {
        var priceChange = PriceChangeFor(Guid.NewGuid(), newPrice: 3.20m);
        _priceRefresher.RefreshUnitPriceAsync(priceChange.ProductId, 3.20m, Arg.Any<CancellationToken>())
            .Returns([]);

        await _consumer.Consume(ConsumeContextFor(priceChange));

        await _cacheInvalidator.DidNotReceiveWithAnyArgs().InvalidateAsync(Guid.Empty, CancellationToken.None);
    }

    private static ProductPriceChangedIntegrationEvent PriceChangeFor(Guid productId, decimal newPrice) => new()
    {
        ProductId = productId,
        ProductSku = "SKU-0001",
        OldPrice = 2.50m,
        NewPrice = newPrice,
    };

    private static ConsumeContext<ProductPriceChangedIntegrationEvent> ConsumeContextFor(
        ProductPriceChangedIntegrationEvent priceChange)
    {
        var consumeContext = Substitute.For<ConsumeContext<ProductPriceChangedIntegrationEvent>>();
        consumeContext.Message.Returns(priceChange);
        consumeContext.CancellationToken.Returns(CancellationToken.None);
        return consumeContext;
    }
}
