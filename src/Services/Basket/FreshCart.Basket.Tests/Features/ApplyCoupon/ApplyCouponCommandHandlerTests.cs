using FluentAssertions;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Features.ShoppingBaskets.ApplyCoupon;
using FreshCart.Basket.Api.Pricing;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Basket.Tests.Support;
using NSubstitute;
using Xunit;

namespace FreshCart.Basket.Tests.Features.ApplyCoupon;

public sealed class ApplyCouponCommandHandlerTests
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryBasketRepository _basketRepository = new();
    private readonly IBasketPricingClient _pricingClient = Substitute.For<IBasketPricingClient>();
    private readonly ApplyCouponCommandHandler _handler;

    public ApplyCouponCommandHandlerTests()
    {
        _handler = new ApplyCouponCommandHandler(
            _basketRepository,
            _pricingClient,
            new FixedTimeProvider(FixedUtcNow));
    }

    [Fact]
    public Task MissingBasketIsRejectedWithNotFound()
    {
        var command = new ApplyCouponCommand(Guid.NewGuid(), "FRESH10");

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        return handlingCommand.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CouponCannotBeAppliedToAnEmptyBasket()
    {
        var command = new ApplyCouponCommand(Guid.NewGuid(), "FRESH10");
        _basketRepository.Seed(ShoppingBasket.CreateForCustomer(command.CustomerId));

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        await handlingCommand.Should().ThrowAsync<BadRequestException>();
        await _pricingClient.DidNotReceiveWithAnyArgs().ValidateCouponAsync(default!, Guid.Empty, default, CancellationToken.None);
    }

    [Fact]
    public async Task RejectedCouponSurfacesTheValidatorsReasonAndStoresNothing()
    {
        var command = new ApplyCouponCommand(Guid.NewGuid(), "EXPIRED5");
        var basket = BasketWithOneLine(command.CustomerId);
        _basketRepository.Seed(basket);

        _pricingClient.ValidateCouponAsync("EXPIRED5", command.CustomerId, basket.StoredSubtotal, Arg.Any<CancellationToken>())
            .Returns(new CouponValidationResult(false, "Coupon 'EXPIRED5' is expired or inactive.", 0m, null));

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        (await handlingCommand.Should().ThrowAsync<BadRequestException>())
            .Which.Detail.Should().Be("Coupon 'EXPIRED5' is expired or inactive.");
        basket.CouponCode.Should().BeNull();
        _basketRepository.MutateWriteCount.Should().Be(0);
    }

    [Fact]
    public async Task AcceptedCouponIsStoredOnTheBasket()
    {
        var command = new ApplyCouponCommand(Guid.NewGuid(), "FRESH10");
        var basket = BasketWithOneLine(command.CustomerId);
        _basketRepository.Seed(basket);

        _pricingClient.ValidateCouponAsync("FRESH10", command.CustomerId, basket.StoredSubtotal, Arg.Any<CancellationToken>())
            .Returns(new CouponValidationResult(true, null, 10m, "Percentage"));

        await _handler.Handle(command, CancellationToken.None);

        var persisted = await _basketRepository.GetAsync(command.CustomerId, CancellationToken.None);
        persisted!.CouponCode.Should().Be("FRESH10");
        persisted.UpdatedOnUtc.Should().Be(FixedUtcNow);
        _basketRepository.MutateWriteCount.Should().Be(1);
    }

    private static ShoppingBasket BasketWithOneLine(Guid customerId)
    {
        var basket = ShoppingBasket.CreateForCustomer(customerId);
        basket.AddOrMergeItem(TestBasketItems.Create(unitPrice: 12.00m, quantity: 2));
        return basket;
    }
}
