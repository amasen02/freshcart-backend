using FluentAssertions;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Persistence;
using FreshCart.Basket.Tests.Support;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace FreshCart.Basket.Tests.Persistence;

public sealed class CachedBasketRepositoryTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IBasketRepository _innerRepository;
    private readonly CachedBasketRepository _cachedRepository;

    public CachedBasketRepositoryTests()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        _serviceProvider = services.BuildServiceProvider();

        _innerRepository = Substitute.For<IBasketRepository>();
        _cachedRepository = new CachedBasketRepository(
            _innerRepository,
            _serviceProvider.GetRequiredService<HybridCache>());
    }

    [Fact]
    public async Task SecondReadIsServedFromCacheWithoutTouchingTheInnerRepository()
    {
        var customerId = Guid.NewGuid();
        var storedBasket = BasketWithOneLine(customerId);
        _innerRepository.GetAsync(customerId, Arg.Any<CancellationToken>()).Returns(storedBasket);

        var firstRead = await _cachedRepository.GetAsync(customerId, CancellationToken.None);
        var secondRead = await _cachedRepository.GetAsync(customerId, CancellationToken.None);

        firstRead!.Id.Should().Be(customerId);
        secondRead!.Id.Should().Be(customerId);
        await _innerRepository.Received(1).GetAsync(customerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertWritesThroughAndEvictsTheCachedEntry()
    {
        var customerId = Guid.NewGuid();
        var storedBasket = BasketWithOneLine(customerId);
        _innerRepository.GetAsync(customerId, Arg.Any<CancellationToken>()).Returns(storedBasket);

        await _cachedRepository.GetAsync(customerId, CancellationToken.None);
        await _cachedRepository.UpsertAsync(storedBasket, CancellationToken.None);
        await _cachedRepository.GetAsync(customerId, CancellationToken.None);

        await _innerRepository.Received(1).UpsertAsync(storedBasket, Arg.Any<CancellationToken>());
        await _innerRepository.Received(2).GetAsync(customerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRemovesTheDocumentAndEvictsTheCachedEntry()
    {
        var customerId = Guid.NewGuid();
        _innerRepository.GetAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(BasketWithOneLine(customerId), (ShoppingBasket?)null);

        await _cachedRepository.GetAsync(customerId, CancellationToken.None);
        await _cachedRepository.DeleteAsync(customerId, CancellationToken.None);
        var readAfterDelete = await _cachedRepository.GetAsync(customerId, CancellationToken.None);

        readAfterDelete.Should().BeNull();
        await _innerRepository.Received(1).DeleteAsync(customerId, Arg.Any<CancellationToken>());
        await _innerRepository.Received(2).GetAsync(customerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MutateDelegatesToTheInnerRepositoryAndEvictsTheCachedEntry()
    {
        var customerId = Guid.NewGuid();
        var storedBasket = BasketWithOneLine(customerId);
        _innerRepository.GetAsync(customerId, Arg.Any<CancellationToken>()).Returns(storedBasket);

        static ShoppingBasket? Mutate(ShoppingBasket? basket) => basket;

        await _cachedRepository.GetAsync(customerId, CancellationToken.None);
        await _cachedRepository.MutateAsync(customerId, Mutate, CancellationToken.None);
        await _cachedRepository.GetAsync(customerId, CancellationToken.None);

        await _innerRepository.Received(1).MutateAsync(customerId, Mutate, Arg.Any<CancellationToken>());
        await _innerRepository.Received(2).GetAsync(customerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArchivePassesStraightThroughToTheInnerRepository()
    {
        var archivedBasket = new ArchivedBasket
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CurrencyCode = BasketDefaults.CurrencyCode,
            Items = [TestBasketItems.Create()],
            Subtotal = 2.50m,
            DiscountTotal = 0m,
            TaxTotal = 0.20m,
            ShippingTotal = 5.99m,
            GrandTotal = 8.69m,
            CheckedOutOnUtc = DateTimeOffset.UtcNow,
        };

        await _cachedRepository.ArchiveAsync(archivedBasket, CancellationToken.None);

        await _innerRepository.Received(1).ArchiveAsync(archivedBasket, Arg.Any<CancellationToken>());
    }

    public void Dispose() => _serviceProvider.Dispose();

    private static ShoppingBasket BasketWithOneLine(Guid customerId)
    {
        var basket = ShoppingBasket.CreateForCustomer(customerId);
        basket.AddOrMergeItem(TestBasketItems.Create());
        return basket;
    }
}
