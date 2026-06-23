using FluentAssertions;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Features.ShoppingBaskets.RemoveCoupon;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Basket.Tests.Support;
using Xunit;

namespace FreshCart.Basket.Tests.Features.RemoveCoupon;

public sealed class RemoveCouponCommandHandlerTests
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryBasketRepository _basketRepository = new();
    private readonly RemoveCouponCommandHandler _handler;

    public RemoveCouponCommandHandlerTests()
    {
        _handler = new RemoveCouponCommandHandler(_basketRepository, new FixedTimeProvider(FixedUtcNow));
    }

    [Fact]
    public Task MissingBasketIsRejectedWithNotFound()
    {
        var command = new RemoveCouponCommand(Guid.NewGuid());

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        return handlingCommand.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemovingWhenNoCouponIsAppliedWritesNothing()
    {
        var command = new RemoveCouponCommand(Guid.NewGuid());
        _basketRepository.Seed(ShoppingBasket.CreateForCustomer(command.CustomerId));

        await _handler.Handle(command, CancellationToken.None);

        _basketRepository.MutateWriteCount.Should().Be(0);
    }

    [Fact]
    public async Task AppliedCouponIsClearedAndTheBasketPersisted()
    {
        var command = new RemoveCouponCommand(Guid.NewGuid());
        var basket = ShoppingBasket.CreateForCustomer(command.CustomerId);
        basket.AddOrMergeItem(TestBasketItems.Create());
        basket.CouponCode = "FRESH10";
        _basketRepository.Seed(basket);

        await _handler.Handle(command, CancellationToken.None);

        var persisted = await _basketRepository.GetAsync(command.CustomerId, CancellationToken.None);
        persisted!.CouponCode.Should().BeNull();
        persisted.UpdatedOnUtc.Should().Be(FixedUtcNow);
        _basketRepository.MutateWriteCount.Should().Be(1);
    }
}
