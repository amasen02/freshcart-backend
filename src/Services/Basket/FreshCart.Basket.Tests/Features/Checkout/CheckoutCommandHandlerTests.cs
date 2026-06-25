using System.Text.Json;
using FluentAssertions;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;
using FreshCart.Basket.Api.Persistence;
using FreshCart.Basket.Api.Pricing;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.Basket.Tests.Support;
using NSubstitute;
using Xunit;

namespace FreshCart.Basket.Tests.Features.Checkout;

public sealed class CheckoutCommandHandlerTests
{
    private const int UuidVersion7 = 7;

    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions WebSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IBasketRepository _basketRepository = Substitute.For<IBasketRepository>();
    private readonly IBasketPricingClient _pricingClient = Substitute.For<IBasketPricingClient>();
    private readonly IBasketUnitOfWork _unitOfWork = Substitute.For<IBasketUnitOfWork>();
    private readonly IBasketCacheInvalidator _cacheInvalidator = Substitute.For<IBasketCacheInvalidator>();
    private readonly CheckoutCommandHandler _handler;

    public CheckoutCommandHandlerTests()
    {
        _handler = new CheckoutCommandHandler(
            _basketRepository,
            _pricingClient,
            _unitOfWork,
            _cacheInvalidator,
            new FixedTimeProvider(FixedUtcNow));
    }

    [Fact]
    public async Task MissingBasketCannotBeCheckedOut()
    {
        var command = CommandFor(Guid.NewGuid());
        _basketRepository.GetAsync(command.CustomerId, Arg.Any<CancellationToken>())
            .Returns((ShoppingBasket?)null);

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        await handlingCommand.Should().ThrowAsync<BadRequestException>();
        await _unitOfWork.DidNotReceiveWithAnyArgs().CommitCheckoutAsync(default!, default!, Guid.Empty, CancellationToken.None);
    }

    [Fact]
    public async Task EmptyBasketCannotBeCheckedOut()
    {
        var command = CommandFor(Guid.NewGuid());
        _basketRepository.GetAsync(command.CustomerId, Arg.Any<CancellationToken>())
            .Returns(ShoppingBasket.CreateForCustomer(command.CustomerId));

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        await handlingCommand.Should().ThrowAsync<BadRequestException>();
        await _pricingClient.DidNotReceiveWithAnyArgs().PriceBasketAsync(default!, default);
    }

    [Fact]
    public async Task PhysicalBasketWithoutAShippingAddressCannotBeCheckedOut()
    {
        var command = CommandFor(Guid.NewGuid(), includeShippingAddress: false);
        var (basket, _) = PhysicalBasketFor(command.CustomerId);
        _basketRepository.GetAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns(basket);

        var handlingCommand = () => _handler.Handle(command, CancellationToken.None);

        await handlingCommand.Should().ThrowAsync<BadRequestException>();
        await _pricingClient.DidNotReceiveWithAnyArgs().PriceBasketAsync(default!, default);
    }

    [Fact]
    public async Task ArchiveOutboxMessageAndLiveBasketDeletionRideOneUnitOfWorkCommit()
    {
        var command = CommandFor(Guid.NewGuid());
        var (basket, productId) = PhysicalBasketFor(command.CustomerId);
        _basketRepository.GetAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns(basket);
        _pricingClient.PriceBasketAsync(Arg.Any<BasketPricingRequest>(), Arg.Any<CancellationToken>())
            .Returns(PricingResultFor(productId));

        var checkoutResult = await _handler.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).CommitCheckoutAsync(
            Arg.Is<ArchivedBasket>(archived => archived.Id == checkoutResult.OrderId),
            Arg.Is<OutboxMessage>(outboxMessage =>
                outboxMessage.EventType == typeof(BasketCheckoutStartedIntegrationEvent).FullName),
            command.CustomerId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BasketIsRepricedBeforeTheCommitAndTheCacheIsEvictedAfterIt()
    {
        var command = CommandFor(Guid.NewGuid());
        var (basket, productId) = PhysicalBasketFor(command.CustomerId);
        _basketRepository.GetAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns(basket);
        _pricingClient.PriceBasketAsync(Arg.Any<BasketPricingRequest>(), Arg.Any<CancellationToken>())
            .Returns(PricingResultFor(productId));

        await _handler.Handle(command, CancellationToken.None);

        Received.InOrder(() =>
        {
            _ = _pricingClient.PriceBasketAsync(Arg.Any<BasketPricingRequest>(), Arg.Any<CancellationToken>());
            _ = _unitOfWork.CommitCheckoutAsync(
                Arg.Any<ArchivedBasket>(),
                Arg.Any<OutboxMessage>(),
                command.CustomerId,
                Arg.Any<CancellationToken>());
            _ = _cacheInvalidator.InvalidateAsync(command.CustomerId, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task OutboxPayloadCarriesTheRepricedCheckoutEvent()
    {
        var command = CommandFor(Guid.NewGuid());
        var (basket, productId) = PhysicalBasketFor(command.CustomerId);
        basket.CouponCode = "FRESH10";
        _basketRepository.GetAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns(basket);
        _pricingClient.PriceBasketAsync(Arg.Any<BasketPricingRequest>(), Arg.Any<CancellationToken>())
            .Returns(PricingResultFor(productId, appliedCoupon: "FRESH10"));

        OutboxMessage? capturedOutboxMessage = null;
        await _unitOfWork.CommitCheckoutAsync(
            Arg.Any<ArchivedBasket>(),
            Arg.Do<OutboxMessage>(outboxMessage => capturedOutboxMessage = outboxMessage),
            command.CustomerId,
            Arg.Any<CancellationToken>());

        var checkoutResult = await _handler.Handle(command, CancellationToken.None);

        checkoutResult.OrderId.Version.Should().Be(UuidVersion7);
        capturedOutboxMessage.Should().NotBeNull();
        capturedOutboxMessage!.OccurredOnUtc.Should().Be(FixedUtcNow);

        var checkoutEvent = JsonSerializer.Deserialize<BasketCheckoutStartedIntegrationEvent>(
            capturedOutboxMessage.ContentJson,
            WebSerializerOptions);

        checkoutEvent.Should().NotBeNull();
        checkoutEvent!.OrderId.Should().Be(checkoutResult.OrderId);
        checkoutEvent.CustomerId.Should().Be(command.CustomerId);
        checkoutEvent.CustomerEmail.Should().Be("shopper@freshcart.local");
        checkoutEvent.CustomerDisplayName.Should().Be("Sam Shopper");
        checkoutEvent.CurrencyCode.Should().Be(BasketDefaults.CurrencyCode);
        checkoutEvent.PaymentMethod.Should().Be(PaymentMethods.CreditCard);
        checkoutEvent.CouponCode.Should().Be("FRESH10");
        checkoutEvent.Subtotal.Should().Be(20.00m);
        checkoutEvent.DiscountTotal.Should().Be(2.00m);
        checkoutEvent.TaxTotal.Should().Be(1.44m);
        checkoutEvent.ShippingTotal.Should().Be(BasketDefaults.StandardShippingFee);
        checkoutEvent.GrandTotal.Should().Be(19.44m + BasketDefaults.StandardShippingFee);
        checkoutEvent.BillingAddress.Should().Be(command.BillingAddress);
        checkoutEvent.ShippingAddress.Should().Be(command.ShippingAddress);

        var eventLine = checkoutEvent.Lines.Should().ContainSingle().Subject;
        eventLine.ProductId.Should().Be(productId);
        eventLine.UnitPrice.Should().Be(10.00m, "the event must carry the live Pricing value, not the stored snapshot");
        eventLine.Quantity.Should().Be(2);
        eventLine.IsDigital.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveSnapshotPreservesLinesAndRepricedTotalsUnderTheOrderId()
    {
        var command = CommandFor(Guid.NewGuid());
        var (basket, productId) = PhysicalBasketFor(command.CustomerId);
        _basketRepository.GetAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns(basket);
        _pricingClient.PriceBasketAsync(Arg.Any<BasketPricingRequest>(), Arg.Any<CancellationToken>())
            .Returns(PricingResultFor(productId));

        ArchivedBasket? capturedArchive = null;
        await _unitOfWork.CommitCheckoutAsync(
            Arg.Do<ArchivedBasket>(archived => capturedArchive = archived),
            Arg.Any<OutboxMessage>(),
            command.CustomerId,
            Arg.Any<CancellationToken>());

        var checkoutResult = await _handler.Handle(command, CancellationToken.None);

        capturedArchive.Should().NotBeNull();
        capturedArchive!.Id.Should().Be(checkoutResult.OrderId);
        capturedArchive.CustomerId.Should().Be(command.CustomerId);
        capturedArchive.Items.Should().ContainSingle().Which.ProductId.Should().Be(productId);
        capturedArchive.Subtotal.Should().Be(20.00m);
        capturedArchive.DiscountTotal.Should().Be(2.00m);
        capturedArchive.TaxTotal.Should().Be(1.44m);
        capturedArchive.ShippingTotal.Should().Be(BasketDefaults.StandardShippingFee);
        capturedArchive.GrandTotal.Should().Be(19.44m + BasketDefaults.StandardShippingFee);
        capturedArchive.CheckedOutOnUtc.Should().Be(FixedUtcNow);
    }

    [Fact]
    public async Task DigitalOnlyBasketShipsFree()
    {
        var command = CommandFor(Guid.NewGuid(), includeShippingAddress: false);
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.CreateForCustomer(command.CustomerId);
        basket.AddOrMergeItem(TestBasketItems.Create(productId, unitPrice: 10.00m, quantity: 2, isDigital: true));
        _basketRepository.GetAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns(basket);
        _pricingClient.PriceBasketAsync(Arg.Any<BasketPricingRequest>(), Arg.Any<CancellationToken>())
            .Returns(PricingResultFor(productId));

        OutboxMessage? capturedOutboxMessage = null;
        await _unitOfWork.CommitCheckoutAsync(
            Arg.Any<ArchivedBasket>(),
            Arg.Do<OutboxMessage>(outboxMessage => capturedOutboxMessage = outboxMessage),
            command.CustomerId,
            Arg.Any<CancellationToken>());

        await _handler.Handle(command, CancellationToken.None);

        var checkoutEvent = JsonSerializer.Deserialize<BasketCheckoutStartedIntegrationEvent>(
            capturedOutboxMessage!.ContentJson,
            WebSerializerOptions);

        checkoutEvent!.ShippingTotal.Should().Be(0m);
        checkoutEvent.GrandTotal.Should().Be(19.44m);
        checkoutEvent.ShippingAddress.Should().BeNull();
    }

    private static CheckoutCommand CommandFor(Guid customerId, bool includeShippingAddress = true)
    {
        var billingAddress = new CheckoutAddress("12 Market Street", null, "Colombo", "00100", "LK");

        return new CheckoutCommand(
            customerId,
            "shopper@freshcart.local",
            "Sam Shopper",
            PaymentMethods.CreditCard,
            billingAddress,
            includeShippingAddress ? billingAddress : null);
    }

    private static (ShoppingBasket Basket, Guid ProductId) PhysicalBasketFor(Guid customerId)
    {
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.CreateForCustomer(customerId);
        basket.AddOrMergeItem(TestBasketItems.Create(productId, unitPrice: 9.50m, quantity: 2));
        return (basket, productId);
    }

    private static BasketPricingResult PricingResultFor(Guid productId, string? appliedCoupon = null) => new(
        [new PricedBasketLine(productId, 10.00m, 9.00m, 18.00m)],
        Subtotal: 20.00m,
        DiscountTotal: 2.00m,
        TaxTotal: 1.44m,
        GrandTotal: 19.44m,
        AppliedCoupon: appliedCoupon);
}
