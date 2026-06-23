using FluentAssertions;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Features.ShoppingBaskets.GetBasket;
using FreshCart.Basket.Api.Persistence;
using FreshCart.Basket.Api.Pricing;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Basket.Tests.Support;
using NSubstitute;
using Xunit;

namespace FreshCart.Basket.Tests.Features.GetBasket;

public sealed class GetBasketQueryHandlerTests
{
    private readonly IBasketRepository _basketRepository = Substitute.For<IBasketRepository>();
    private readonly IBasketPricingClient _pricingClient = Substitute.For<IBasketPricingClient>();
    private readonly GetBasketQueryHandler _handler;

    public GetBasketQueryHandlerTests()
    {
        _handler = new GetBasketQueryHandler(_basketRepository, _pricingClient);
    }

    [Fact]
    public async Task MissingBasketReturnsAnEmptyDtoWithoutCallingPricing()
    {
        var customerId = Guid.NewGuid();
        _basketRepository.GetAsync(customerId, Arg.Any<CancellationToken>()).Returns((ShoppingBasket?)null);

        var basketDto = await _handler.Handle(new GetBasketQuery(customerId), CancellationToken.None);

        basketDto.Should().BeEquivalentTo(BasketDto.EmptyFor(customerId));
        await _pricingClient.DidNotReceiveWithAnyArgs().PriceBasketAsync(default!, default);
    }

    [Fact]
    public async Task BasketWithNoLinesReturnsAnEmptyDtoWithoutCallingPricing()
    {
        var customerId = Guid.NewGuid();
        _basketRepository.GetAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(ShoppingBasket.CreateForCustomer(customerId));

        var basketDto = await _handler.Handle(new GetBasketQuery(customerId), CancellationToken.None);

        basketDto.Items.Should().BeEmpty();
        basketDto.GrandTotal.Should().Be(0m);
        await _pricingClient.DidNotReceiveWithAnyArgs().PriceBasketAsync(default!, default);
    }

    [Fact]
    public async Task PopulatedBasketIsRepricedAndMappedWithLivePerLineValues()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.CreateForCustomer(customerId);
        basket.AddOrMergeItem(TestBasketItems.Create(productId, unitPrice: 2.50m, quantity: 4));
        basket.CouponCode = "FRESH10";
        _basketRepository.GetAsync(customerId, Arg.Any<CancellationToken>()).Returns(basket);

        _pricingClient.PriceBasketAsync(Arg.Any<BasketPricingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new BasketPricingResult(
                [new PricedBasketLine(productId, 2.50m, 2.25m, 9.00m)],
                Subtotal: 10.00m,
                DiscountTotal: 1.00m,
                TaxTotal: 0.72m,
                GrandTotal: 9.72m,
                AppliedCoupon: "FRESH10"));

        var basketDto = await _handler.Handle(new GetBasketQuery(customerId), CancellationToken.None);

        var lineDto = basketDto.Items.Should().ContainSingle().Subject;
        lineDto.UnitPrice.Should().Be(2.50m);
        lineDto.DiscountedUnitPrice.Should().Be(2.25m);
        lineDto.LineTotal.Should().Be(9.00m);
        basketDto.Subtotal.Should().Be(10.00m);
        basketDto.DiscountTotal.Should().Be(1.00m);
        basketDto.TaxTotal.Should().Be(0.72m);
        basketDto.GrandTotal.Should().Be(9.72m);
        basketDto.AppliedCoupon.Should().Be("FRESH10");

        await _pricingClient.Received(1).PriceBasketAsync(
            Arg.Is<BasketPricingRequest>(request =>
                request.CustomerId == customerId &&
                request.CouponCode == "FRESH10" &&
                request.Lines.Count == 1 &&
                request.Lines[0].ProductId == productId &&
                request.Lines[0].Quantity == 4),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TotalsAreRoundedToTwoDecimalsWithBankersRounding()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.CreateForCustomer(customerId);
        basket.AddOrMergeItem(TestBasketItems.Create(productId));
        _basketRepository.GetAsync(customerId, Arg.Any<CancellationToken>()).Returns(basket);

        _pricingClient.PriceBasketAsync(Arg.Any<BasketPricingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new BasketPricingResult(
                [new PricedBasketLine(productId, 2.505m, 2.505m, 2.505m)],
                Subtotal: 10.125m,
                DiscountTotal: 0m,
                TaxTotal: 0.815m,
                GrandTotal: 10.135m,
                AppliedCoupon: null));

        var basketDto = await _handler.Handle(new GetBasketQuery(customerId), CancellationToken.None);

        basketDto.Subtotal.Should().Be(10.12m);
        basketDto.TaxTotal.Should().Be(0.82m);
        basketDto.GrandTotal.Should().Be(10.14m);
    }

    [Fact]
    public Task MissingPricedLineForABasketItemIsAnInternalFault()
    {
        var customerId = Guid.NewGuid();
        var basket = ShoppingBasket.CreateForCustomer(customerId);
        basket.AddOrMergeItem(TestBasketItems.Create());
        _basketRepository.GetAsync(customerId, Arg.Any<CancellationToken>()).Returns(basket);

        _pricingClient.PriceBasketAsync(Arg.Any<BasketPricingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new BasketPricingResult([], 0m, 0m, 0m, 0m, null));

        var handlingQuery = () => _handler.Handle(new GetBasketQuery(customerId), CancellationToken.None);

        return handlingQuery.Should().ThrowAsync<InternalServerException>();
    }
}
