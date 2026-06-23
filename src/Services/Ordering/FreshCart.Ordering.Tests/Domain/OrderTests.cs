using FluentAssertions;
using FreshCart.Ordering.Domain.Exceptions;
using FreshCart.Ordering.Domain.Orders;
using FreshCart.Ordering.Domain.Orders.Events;
using FreshCart.Ordering.Tests.Support;

namespace FreshCart.Ordering.Tests.Domain;

public sealed class OrderTests
{
    private static readonly DateTimeOffset ConfirmedOnUtc = new(2026, 6, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CancelledOnUtc = new(2026, 6, 18, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SubmitCreatesOrderInSubmittedStateWithCopiedLines()
    {
        var submission = OrderTestData.ValidSubmission();

        var order = Order.Submit(submission);

        order.Id.Should().Be(submission.OrderId);
        order.CustomerId.Should().Be(submission.CustomerId);
        order.Status.Should().Be(OrderStatus.Submitted);
        order.Lines.Should().HaveCount(submission.Lines.Count);
        order.GrandTotal.Should().Be(submission.GrandTotal);
    }

    [Fact]
    public void SubmitRaisesOrderSubmittedDomainEvent()
    {
        var order = OrderTestData.SubmittedOrder();

        var domainEvents = order.DequeueDomainEvents();

        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderSubmittedDomainEvent>()
            .Which.OrderId.Should().Be(order.Id);
    }

    [Fact]
    public void DequeueDomainEventsClearsTheQueueSoEventsAreNotPublishedTwice()
    {
        var order = OrderTestData.SubmittedOrder();

        order.DequeueDomainEvents();
        var secondDequeue = order.DequeueDomainEvents();

        secondDequeue.Should().BeEmpty();
    }

    [Fact]
    public void SubmitThrowsWhenGrandTotalDoesNotBalance()
    {
        var submission = OrderTestData.ValidSubmission() with
        {
            GrandTotal = new Money(99.99m, OrderTestData.CurrencyCode),
        };

        var act = () => Order.Submit(submission);

        act.Should().Throw<OrderDomainException>().WithMessage("*grand total*");
    }

    [Fact]
    public void SubmitThrowsWhenThereAreNoLines()
    {
        var submission = OrderTestData.ValidSubmission() with { Lines = [] };

        var act = () => Order.Submit(submission);

        act.Should().Throw<OrderDomainException>().WithMessage("*at least one line*");
    }

    [Fact]
    public void SubmitThrowsWhenAnyLineQuantityIsNotPositive()
    {
        var submission = OrderTestData.ValidSubmission();
        var zeroQuantityLine = submission.Lines[0] with { Quantity = 0 };
        var withZeroQuantity = submission with { Lines = [zeroQuantityLine, submission.Lines[1]] };

        var act = () => Order.Submit(withZeroQuantity);

        act.Should().Throw<OrderDomainException>().WithMessage("*positive quantity*");
    }

    [Fact]
    public void SubmitThrowsWhenCustomerIdentifierIsEmpty()
    {
        var submission = OrderTestData.ValidSubmission() with { CustomerId = Guid.Empty };

        var act = () => Order.Submit(submission);

        act.Should().Throw<OrderDomainException>().WithMessage("*identifier*");
    }

    [Fact]
    public void SubmitThrowsWhenPaymentMethodIsBlank()
    {
        var submission = OrderTestData.ValidSubmission() with { PaymentMethod = "   " };

        var act = () => Order.Submit(submission);

        act.Should().Throw<OrderDomainException>().WithMessage("*payment method*");
    }

    [Fact]
    public void MarkStockReservedMovesFromSubmittedToStockReserved()
    {
        var order = OrderTestData.SubmittedOrder();
        var reservationId = Guid.NewGuid();

        order.MarkStockReserved(reservationId);

        order.Status.Should().Be(OrderStatus.StockReserved);
        order.ReservationId.Should().Be(reservationId);
    }

    [Fact]
    public void MarkStockReservedThrowsWhenNotSubmitted()
    {
        var order = OrderTestData.StockReservedOrder(Guid.NewGuid());

        var act = () => order.MarkStockReserved(Guid.NewGuid());

        act.Should().Throw<OrderDomainException>().WithMessage("*must be Submitted*");
    }

    [Fact]
    public void MarkPaidMovesFromStockReservedToPaid()
    {
        var order = OrderTestData.StockReservedOrder(Guid.NewGuid());
        var paymentId = Guid.NewGuid();

        order.MarkPaid(paymentId);

        order.Status.Should().Be(OrderStatus.Paid);
        order.PaymentId.Should().Be(paymentId);
    }

    [Fact]
    public void MarkPaidThrowsWhenStockHasNotBeenReserved()
    {
        var order = OrderTestData.SubmittedOrder();

        var act = () => order.MarkPaid(Guid.NewGuid());

        act.Should().Throw<OrderDomainException>().WithMessage("*must be StockReserved*");
    }

    [Fact]
    public void ConfirmMovesFromPaidToConfirmedAndRaisesEvent()
    {
        var order = OrderTestData.StockReservedOrder(Guid.NewGuid());
        order.MarkPaid(Guid.NewGuid());
        order.DequeueDomainEvents();

        order.Confirm(ConfirmedOnUtc);

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.ConfirmedOnUtc.Should().Be(ConfirmedOnUtc);
        order.DequeueDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<OrderConfirmedDomainEvent>();
    }

    [Fact]
    public void ConfirmThrowsWhenNotPaid()
    {
        var order = OrderTestData.StockReservedOrder(Guid.NewGuid());

        var act = () => order.Confirm(ConfirmedOnUtc);

        act.Should().Throw<OrderDomainException>().WithMessage("*must be Paid*");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelMovesToCancelledFromAnyPreConfirmationStateAndRaisesEvent(bool afterStockReserved)
    {
        var order = afterStockReserved
            ? OrderTestData.StockReservedOrder(Guid.NewGuid())
            : OrderTestData.SubmittedOrder();
        order.DequeueDomainEvents();

        order.Cancel("Customer changed their mind", CancelledOnUtc);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.FailureReason.Should().Be("Customer changed their mind");
        order.CancelledOnUtc.Should().Be(CancelledOnUtc);
        order.DequeueDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<OrderCancelledDomainEvent>();
    }

    [Fact]
    public void CancelThrowsWhenOrderIsAlreadyConfirmed()
    {
        var order = OrderTestData.ConfirmedOrder(Guid.NewGuid(), Guid.NewGuid(), ConfirmedOnUtc);

        var act = () => order.Cancel("Too late", CancelledOnUtc);

        act.Should().Throw<OrderDomainException>().WithMessage("*already Confirmed*");
    }

    [Fact]
    public void CancelThrowsWhenReasonIsBlank()
    {
        var order = OrderTestData.SubmittedOrder();

        var act = () => order.Cancel("   ", CancelledOnUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RefundMovesFromConfirmedToRefundedAndRaisesEvent()
    {
        var order = OrderTestData.ConfirmedOrder(Guid.NewGuid(), Guid.NewGuid(), ConfirmedOnUtc);
        order.DequeueDomainEvents();

        order.Refund("Damaged on delivery", CancelledOnUtc);

        order.Status.Should().Be(OrderStatus.Refunded);
        order.DequeueDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<OrderRefundedDomainEvent>()
            .Which.RefundAmount.Should().Be(order.GrandTotal);
    }

    [Fact]
    public void RefundThrowsWhenOrderIsNotConfirmed()
    {
        var order = OrderTestData.StockReservedOrder(Guid.NewGuid());

        var act = () => order.Refund("No charge to refund", CancelledOnUtc);

        act.Should().Throw<OrderDomainException>().WithMessage("*must be Confirmed*");
    }
}
