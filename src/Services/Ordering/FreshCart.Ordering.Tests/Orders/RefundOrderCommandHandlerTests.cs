using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Orders.Commands.RefundOrder;
using FreshCart.Ordering.Domain.Exceptions;
using FreshCart.Ordering.Domain.Orders;
using FreshCart.Ordering.Tests.Support;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace FreshCart.Ordering.Tests.Orders;

public sealed class RefundOrderCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 14, 0, 0, TimeSpan.Zero);

    private readonly IOrderRepository orderRepository = Substitute.For<IOrderRepository>();
    private readonly IPaymentClient paymentClient = Substitute.For<IPaymentClient>();
    private readonly RefundOrderCommandHandler handler;

    public RefundOrderCommandHandlerTests() =>
        handler = new RefundOrderCommandHandler(orderRepository, paymentClient, new FixedTimeProvider(Now));

    [Fact]
    public async Task RefundsThroughTheProviderAndSavesWhenTheOrderIsConfirmed()
    {
        var paymentId = Guid.NewGuid();
        var order = OrderTestData.ConfirmedOrder(Guid.NewGuid(), paymentId, Now);
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        paymentClient
            .RefundPaymentAsync(Arg.Any<PaymentRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentRefundResult(Succeeded: true, order.GrandTotal.Amount, FailureReason: null));

        await handler.Handle(new RefundOrderCommand(order.Id, "Damaged"), CancellationToken.None);

        await paymentClient.Received(1).RefundPaymentAsync(
            Arg.Is<PaymentRefundRequest>(request =>
                request.PaymentId == paymentId && request.Amount == order.GrandTotal.Amount),
            Arg.Any<CancellationToken>());
        await orderRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ThrowsConflictAndDoesNotSaveWhenTheProviderDeclinesTheRefund()
    {
        var order = OrderTestData.ConfirmedOrder(Guid.NewGuid(), Guid.NewGuid(), Now);
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        paymentClient
            .RefundPaymentAsync(Arg.Any<PaymentRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentRefundResult(Succeeded: false, RefundedAmount: 0m, "Provider rejected"));

        var act = () => handler.Handle(new RefundOrderCommand(order.Id, "Damaged"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await orderRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public Task ThrowsNotFoundWhenOrderDoesNotExist()
    {
        var orderId = Guid.NewGuid();
        orderRepository.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => handler.Handle(new RefundOrderCommand(orderId, "Gone"), CancellationToken.None);

        return act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsDomainExceptionAndNeverCallsTheProviderWhenOrderIsNotConfirmed()
    {
        var order = OrderTestData.StockReservedOrder(Guid.NewGuid());
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var act = () => handler.Handle(new RefundOrderCommand(order.Id, "Too early"), CancellationToken.None);

        await act.Should().ThrowAsync<OrderDomainException>();
        await paymentClient.DidNotReceive().RefundPaymentAsync(Arg.Any<PaymentRefundRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public Task TreatsAConcurrentRefundAsAnIdempotentSuccessWhenTheOrderIsAlreadyRefunded()
    {
        var order = OrderTestData.ConfirmedOrder(Guid.NewGuid(), Guid.NewGuid(), Now);
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        paymentClient
            .RefundPaymentAsync(Arg.Any<PaymentRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentRefundResult(Succeeded: true, order.GrandTotal.Amount, FailureReason: null));
        orderRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConflictException("The order was modified by another operation.")));
        orderRepository.GetPersistedStatusAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(OrderStatus.Refunded);

        var act = () => handler.Handle(new RefundOrderCommand(order.Id, "Damaged"), CancellationToken.None);

        return act.Should().NotThrowAsync();
    }

    [Fact]
    public Task RethrowsTheConflictWhenTheOrderWasNotRefundedByTheWinningTransaction()
    {
        var order = OrderTestData.ConfirmedOrder(Guid.NewGuid(), Guid.NewGuid(), Now);
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        paymentClient
            .RefundPaymentAsync(Arg.Any<PaymentRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentRefundResult(Succeeded: true, order.GrandTotal.Amount, FailureReason: null));
        orderRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConflictException("The order was modified by another operation.")));
        orderRepository.GetPersistedStatusAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(OrderStatus.Confirmed);

        var act = () => handler.Handle(new RefundOrderCommand(order.Id, "Damaged"), CancellationToken.None);

        return act.Should().ThrowAsync<ConflictException>();
    }
}
