using FluentAssertions;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Features.ShoppingBaskets.RemoveItem;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Basket.Tests.Support;
using Xunit;

namespace FreshCart.Basket.Tests.Features.RemoveItem;

public sealed class RemoveItemCommandHandlerTests
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryBasketRepository _basketRepository = new();
    private readonly RemoveItemCommandHandler _handler;

    public RemoveItemCommandHandlerTests()
    {
        _handler = new RemoveItemCommandHandler(_basketRepository, new FixedTimeProvider(FixedUtcNow));
    }

    [Fact]
    public Task MissingBasketIsRejectedWithNotFound()
    {
        var command = new RemoveItemCommand(Guid.NewGuid(), Guid.NewGuid());

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        return handlingCommand.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MissingLineIsRejectedWithNotFound()
    {
        var command = new RemoveItemCommand(Guid.NewGuid(), Guid.NewGuid());
        _basketRepository.Seed(ShoppingBasket.CreateForCustomer(command.CustomerId));

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        await handlingCommand.Should().ThrowAsync<NotFoundException>();
        _basketRepository.MutateWriteCount.Should().Be(0);
    }

    [Fact]
    public async Task ExistingLineIsRemovedAndTheBasketPersisted()
    {
        var command = new RemoveItemCommand(Guid.NewGuid(), Guid.NewGuid());
        var basket = ShoppingBasket.CreateForCustomer(command.CustomerId);
        basket.AddOrMergeItem(TestBasketItems.Create(command.ProductId));
        _basketRepository.Seed(basket);

        await _handler.Handle(command, CancellationToken.None);

        var persisted = await _basketRepository.GetAsync(command.CustomerId, CancellationToken.None);
        persisted!.IsEmpty.Should().BeTrue();
        persisted.UpdatedOnUtc.Should().Be(FixedUtcNow);
        _basketRepository.MutateWriteCount.Should().Be(1);
    }
}
