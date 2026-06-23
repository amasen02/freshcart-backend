using FluentAssertions;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Features.ShoppingBaskets.UpdateItemQuantity;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Basket.Tests.Support;
using Xunit;

namespace FreshCart.Basket.Tests.Features.UpdateItemQuantity;

public sealed class UpdateItemQuantityCommandHandlerTests
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryBasketRepository _basketRepository = new();
    private readonly UpdateItemQuantityCommandHandler _handler;

    public UpdateItemQuantityCommandHandlerTests()
    {
        _handler = new UpdateItemQuantityCommandHandler(_basketRepository, new FixedTimeProvider(FixedUtcNow));
    }

    [Fact]
    public Task MissingBasketIsRejectedWithNotFound()
    {
        var command = new UpdateItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 2);

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        return handlingCommand.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MissingLineIsRejectedWithNotFound()
    {
        var command = new UpdateItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 2);
        _basketRepository.Seed(ShoppingBasket.CreateForCustomer(command.CustomerId));

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        await handlingCommand.Should().ThrowAsync<NotFoundException>();
        _basketRepository.MutateWriteCount.Should().Be(0);
    }

    [Fact]
    public async Task PositiveQuantityIsAppliedToTheLine()
    {
        var command = new UpdateItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 8);
        var basket = ShoppingBasket.CreateForCustomer(command.CustomerId);
        basket.AddOrMergeItem(TestBasketItems.Create(command.ProductId, quantity: 3));
        _basketRepository.Seed(basket);

        await _handler.Handle(command, CancellationToken.None);

        var persisted = await _basketRepository.GetAsync(command.CustomerId, CancellationToken.None);
        persisted!.Items.Should().ContainSingle().Which.Quantity.Should().Be(8);
        persisted.UpdatedOnUtc.Should().Be(FixedUtcNow);
        _basketRepository.MutateWriteCount.Should().Be(1);
    }

    [Fact]
    public async Task ZeroQuantityRemovesTheLine()
    {
        var command = new UpdateItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 0);
        var basket = ShoppingBasket.CreateForCustomer(command.CustomerId);
        basket.AddOrMergeItem(TestBasketItems.Create(command.ProductId, quantity: 3));
        _basketRepository.Seed(basket);

        await _handler.Handle(command, CancellationToken.None);

        var persisted = await _basketRepository.GetAsync(command.CustomerId, CancellationToken.None);
        persisted!.IsEmpty.Should().BeTrue();
        _basketRepository.MutateWriteCount.Should().Be(1);
    }
}
