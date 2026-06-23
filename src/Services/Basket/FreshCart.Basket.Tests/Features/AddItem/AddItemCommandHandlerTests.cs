using FluentAssertions;
using FreshCart.Basket.Api.Catalog;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Features.ShoppingBaskets.AddItem;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Basket.Tests.Support;
using NSubstitute;
using Xunit;

namespace FreshCart.Basket.Tests.Features.AddItem;

public sealed class AddItemCommandHandlerTests
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryBasketRepository _basketRepository = new();
    private readonly ICatalogProductClient _catalogProductClient = Substitute.For<ICatalogProductClient>();
    private readonly AddItemCommandHandler _handler;

    public AddItemCommandHandlerTests()
    {
        _handler = new AddItemCommandHandler(
            _basketRepository,
            _catalogProductClient,
            new FixedTimeProvider(FixedUtcNow));
    }

    [Fact]
    public async Task UnknownProductIsRejectedWithNotFound()
    {
        var command = new AddItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1);
        _catalogProductClient.GetProductAsync(command.ProductId, Arg.Any<CancellationToken>())
            .Returns((CatalogProduct?)null);

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        await handlingCommand.Should().ThrowAsync<NotFoundException>();
        _basketRepository.MutateWriteCount.Should().Be(0);
    }

    [Fact]
    public async Task InactiveProductCannotBeAdded()
    {
        var command = new AddItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1);
        _catalogProductClient.GetProductAsync(command.ProductId, Arg.Any<CancellationToken>())
            .Returns(CatalogProductFor(command.ProductId, isActive: false));

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        await handlingCommand.Should().ThrowAsync<BadRequestException>();
        _basketRepository.MutateWriteCount.Should().Be(0);
    }

    [Fact]
    public async Task FirstItemCreatesTheBasketAndCapturesTheCatalogSnapshot()
    {
        var command = new AddItemCommand(Guid.NewGuid(), Guid.NewGuid(), 2);
        _catalogProductClient.GetProductAsync(command.ProductId, Arg.Any<CancellationToken>())
            .Returns(CatalogProductFor(command.ProductId, isActive: true));

        await _handler.Handle(command, CancellationToken.None);

        var persistedBasket = await _basketRepository.GetAsync(command.CustomerId, CancellationToken.None);
        persistedBasket.Should().NotBeNull();
        persistedBasket!.Id.Should().Be(command.CustomerId);
        persistedBasket.UpdatedOnUtc.Should().Be(FixedUtcNow);

        var persistedLine = persistedBasket.Items.Should().ContainSingle().Subject;
        persistedLine.ProductId.Should().Be(command.ProductId);
        persistedLine.ProductSku.Should().Be("SKU-7001");
        persistedLine.ProductName.Should().Be("Cold brew coffee 1L");
        persistedLine.PrimaryCategory.Should().Be("Beverages");
        persistedLine.UnitPrice.Should().Be(6.40m);
        persistedLine.Quantity.Should().Be(2);
        persistedLine.ImageUrl.Should().Be("https://cdn.freshcart.local/products/cold-brew.jpg");
        persistedLine.IsDigital.Should().BeFalse();
    }

    [Fact]
    public async Task AddingAProductAlreadyInTheBasketMergesQuantitiesUpToTheCap()
    {
        var command = new AddItemCommand(Guid.NewGuid(), Guid.NewGuid(), 10);
        _catalogProductClient.GetProductAsync(command.ProductId, Arg.Any<CancellationToken>())
            .Returns(CatalogProductFor(command.ProductId, isActive: true));

        var existingBasket = ShoppingBasket.CreateForCustomer(command.CustomerId);
        existingBasket.AddOrMergeItem(TestBasketItems.Create(command.ProductId, quantity: 95));
        _basketRepository.Seed(existingBasket);

        await _handler.Handle(command, CancellationToken.None);

        var persistedBasket = await _basketRepository.GetAsync(command.CustomerId, CancellationToken.None);
        persistedBasket!.Items.Should().ContainSingle().Which.Quantity.Should().Be(ShoppingBasket.MaxQuantityPerLine);
        _basketRepository.MutateWriteCount.Should().Be(1);
    }

    [Fact]
    public async Task AConcurrentAddIsMergedAgainstTheWinningSnapshotRatherThanOverwritingIt()
    {
        var command = new AddItemCommand(Guid.NewGuid(), Guid.NewGuid(), 3);
        _catalogProductClient.GetProductAsync(command.ProductId, Arg.Any<CancellationToken>())
            .Returns(CatalogProductFor(command.ProductId, isActive: true));

        var otherProductId = Guid.NewGuid();
        var winningSnapshot = ShoppingBasket.CreateForCustomer(command.CustomerId);
        winningSnapshot.AddOrMergeItem(TestBasketItems.Create(otherProductId, quantity: 4));
        _basketRepository.InjectSingleConflict(winningSnapshot);

        await _handler.Handle(command, CancellationToken.None);

        var persistedBasket = await _basketRepository.GetAsync(command.CustomerId, CancellationToken.None);
        persistedBasket!.Items.Should().HaveCount(2, "the concurrently added line must survive the merge");
        persistedBasket.Items.Single(item => item.ProductId == command.ProductId).Quantity.Should().Be(3);
        persistedBasket.Items.Single(item => item.ProductId == otherProductId).Quantity.Should().Be(4);
    }

    private static CatalogProduct CatalogProductFor(Guid productId, bool isActive) => new(
        productId,
        "SKU-7001",
        "Cold brew coffee 1L",
        "Beverages",
        6.40m,
        "https://cdn.freshcart.local/products/cold-brew.jpg",
        IsDigital: false,
        IsActive: isActive);
}
