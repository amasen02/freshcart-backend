using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Orders.Commands.CancelOrder;
using FreshCart.Ordering.Domain.Orders;
using FreshCart.Ordering.Tests.Support;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace FreshCart.Ordering.Tests.Orders;

public sealed class CancelOrderCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 13, 0, 0, TimeSpan.Zero);

    private readonly IOrderRepository orderRepository = Substitute.For<IOrderRepository>();
    private readonly IInventoryClient inventoryClient = Substitute.For<IInventoryClient>();
    private readonly CancelOrderCommandHandler handler;

    public CancelOrderCommandHandlerTests() =>
        handler = new CancelOrderCommandHandler(orderRepository, inventoryClient, new FixedTimeProvider(Now));

    [Fact]
    public async Task CancelsTheOrderAndReleasesReservationWhenStockWasReserved()
    {
        var customerId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var order = OrderTestData.SubmittedOrder(customerId: customerId);
        order.MarkStockReserved(reservationId);
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await handler.Handle(new CancelOrderCommand(order.Id, customerId, "Changed my mind"), CancellationToken.None);

        await inventoryClient.Received(1).ReleaseReservationAsync(order.Id, Arg.Any<CancellationToken>());
        await orderRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotReleaseReservationWhenNoneWasTaken()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestData.SubmittedOrder(customerId: customerId);
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await handler.Handle(new CancelOrderCommand(order.Id, customerId, "Changed my mind"), CancellationToken.None);

        await inventoryClient.DidNotReceive().ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public Task ThrowsNotFoundWhenOrderDoesNotExist()
    {
        var orderId = Guid.NewGuid();
        orderRepository.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => handler.Handle(new CancelOrderCommand(orderId, Guid.NewGuid(), "Gone"), CancellationToken.None);

        return act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsForbiddenWhenCallerIsNotTheOwner()
    {
        var order = OrderTestData.SubmittedOrder(customerId: Guid.NewGuid());
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var act = () => handler.Handle(
            new CancelOrderCommand(order.Id, Guid.NewGuid(), "Not mine"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await orderRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public Task TreatsAConcurrentSagaCancelAsAnIdempotentSuccessWhenTheOrderIsAlreadyCancelled()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestData.SubmittedOrder(customerId: customerId);
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        orderRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConflictException("The order was modified by another operation.")));
        orderRepository.GetPersistedStatusAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(OrderStatus.Cancelled);

        var act = () => handler.Handle(new CancelOrderCommand(order.Id, customerId, "Changed my mind"), CancellationToken.None);

        return act.Should().NotThrowAsync();
    }

    [Fact]
    public Task RethrowsTheConflictWhenTheWinningTransitionLeftTheOrderInANonCancelledState()
    {
        var customerId = Guid.NewGuid();
        var order = OrderTestData.SubmittedOrder(customerId: customerId);
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        orderRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConflictException("The order was modified by another operation.")));
        orderRepository.GetPersistedStatusAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(OrderStatus.Confirmed);

        var act = () => handler.Handle(new CancelOrderCommand(order.Id, customerId, "Changed my mind"), CancellationToken.None);

        return act.Should().ThrowAsync<ConflictException>();
    }
}
