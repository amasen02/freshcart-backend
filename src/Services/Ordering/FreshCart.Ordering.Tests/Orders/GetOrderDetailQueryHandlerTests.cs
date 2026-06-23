using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Orders.Dtos;
using FreshCart.Ordering.Application.Orders.Queries.GetOrderDetail;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace FreshCart.Ordering.Tests.Orders;

public sealed class GetOrderDetailQueryHandlerTests
{
    private readonly IOrderReadQueries readQueries = Substitute.For<IOrderReadQueries>();
    private readonly GetOrderDetailQueryHandler handler;

    public GetOrderDetailQueryHandlerTests() => handler = new GetOrderDetailQueryHandler(readQueries);

    [Fact]
    public async Task ReturnsTheOrderWhenTheCallerIsTheOwner()
    {
        var customerId = Guid.NewGuid();
        var detail = DetailFor(Guid.NewGuid(), customerId);
        readQueries.GetOrderDetailAsync(detail.OrderId, Arg.Any<CancellationToken>()).Returns(detail);

        var result = await handler.Handle(
            new GetOrderDetailQuery(detail.OrderId, customerId, IsAdministrator: false),
            CancellationToken.None);

        result.Should().BeSameAs(detail);
    }

    [Fact]
    public async Task ReturnsTheOrderForAnAdministratorWhoDoesNotOwnIt()
    {
        var detail = DetailFor(Guid.NewGuid(), Guid.NewGuid());
        readQueries.GetOrderDetailAsync(detail.OrderId, Arg.Any<CancellationToken>()).Returns(detail);

        var result = await handler.Handle(
            new GetOrderDetailQuery(detail.OrderId, Guid.NewGuid(), IsAdministrator: true),
            CancellationToken.None);

        result.Should().BeSameAs(detail);
    }

    [Fact]
    public Task ThrowsForbiddenWhenANonOwnerNonAdministratorRequestsTheOrder()
    {
        var detail = DetailFor(Guid.NewGuid(), Guid.NewGuid());
        readQueries.GetOrderDetailAsync(detail.OrderId, Arg.Any<CancellationToken>()).Returns(detail);

        var act = () => handler.Handle(
            new GetOrderDetailQuery(detail.OrderId, Guid.NewGuid(), IsAdministrator: false),
            CancellationToken.None);

        return act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public Task ThrowsNotFoundWhenTheOrderDoesNotExist()
    {
        var orderId = Guid.NewGuid();
        readQueries.GetOrderDetailAsync(orderId, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => handler.Handle(
            new GetOrderDetailQuery(orderId, Guid.NewGuid(), IsAdministrator: true),
            CancellationToken.None);

        return act.Should().ThrowAsync<NotFoundException>();
    }

    private static OrderDetailDto DetailFor(Guid orderId, Guid customerId) => new()
    {
        OrderId = orderId,
        CustomerId = customerId,
        Status = "Confirmed",
        CustomerEmail = "shopper@freshcart.local",
        CustomerDisplayName = "Sample Shopper",
        PaymentMethod = "Card",
        Subtotal = 12.80m,
        DiscountTotal = 2.00m,
        TaxTotal = 1.10m,
        ShippingTotal = 5.00m,
        GrandTotal = 16.90m,
        CurrencyCode = "USD",
        BillingAddress = new OrderAddressDto("12 Market Street", null, "Springfield", "12345", "US"),
        SubmittedOnUtc = new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero),
        Lines = [],
    };
}
