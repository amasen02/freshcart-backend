using FreshCart.Ordering.Domain.Exceptions;
using FreshCart.Ordering.Domain.Orders.Events;
using FreshCart.Ordering.Domain.Primitives;

namespace FreshCart.Ordering.Domain.Orders;

/// <summary>
/// Aggregate root for the order lifecycle: Submitted, StockReserved, Paid, Confirmed, with
/// Cancelled reachable before confirmation and Refunded only after it. Every transition is an
/// intention-revealing method that enforces the legal predecessor state, so an order can never
/// be observed in an impossible shape regardless of message ordering in the checkout saga.
/// </summary>
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private Order()
    {
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string CustomerEmail { get; private set; } = string.Empty;

    public string CustomerDisplayName { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public Money Subtotal { get; private set; } = null!;

    public Money DiscountTotal { get; private set; } = null!;

    public Money TaxTotal { get; private set; } = null!;

    public Money ShippingTotal { get; private set; } = null!;

    public Money GrandTotal { get; private set; } = null!;

    public string PaymentMethod { get; private set; } = string.Empty;

    public Address BillingAddress { get; private set; } = null!;

    public Address? ShippingAddress { get; private set; }

    public Guid? ReservationId { get; private set; }

    public Guid? PaymentId { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset SubmittedOnUtc { get; private set; }

    public DateTimeOffset? ConfirmedOnUtc { get; private set; }

    public DateTimeOffset? CancelledOnUtc { get; private set; }

    // Optimistic concurrency token. A user-initiated cancel/refund can race the checkout saga's own
    // compensation on the same row; the token forces the losing transaction to fail rather than
    // silently re-emit OrderCancelled/OrderRefunded and re-release stock. EF owns it; the domain
    // never assigns it.
    public byte[] RowVersion { get; private set; } = [];

    public static Order Submit(OrderSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        EnsureIdentifiersArePresent(submission);
        EnsureCustomerContactIsPresent(submission);
        EnsureLinesAreOrderable(submission.Lines);
        EnsureTotalsBalance(submission);

        var order = new Order
        {
            Id = submission.OrderId,
            CustomerId = submission.CustomerId,
            CustomerEmail = submission.CustomerEmail,
            CustomerDisplayName = submission.CustomerDisplayName,
            Status = OrderStatus.Submitted,
            Subtotal = submission.Subtotal,
            DiscountTotal = submission.DiscountTotal,
            TaxTotal = submission.TaxTotal,
            ShippingTotal = submission.ShippingTotal,
            GrandTotal = submission.GrandTotal,
            PaymentMethod = submission.PaymentMethod,
            BillingAddress = submission.BillingAddress,
            ShippingAddress = submission.ShippingAddress,
            SubmittedOnUtc = submission.SubmittedOnUtc,
        };

        order._lines.AddRange(submission.Lines);

        order.RaiseDomainEvent(new OrderSubmittedDomainEvent(
            order.Id,
            order.CustomerId,
            order.CustomerEmail,
            order.CustomerDisplayName,
            order.GrandTotal,
            order.SubmittedOnUtc));

        return order;
    }

    public void MarkStockReserved(Guid reservationId)
    {
        EnsureCurrentStatusIs(OrderStatus.Submitted, "mark stock as reserved for");

        ReservationId = reservationId;
        Status = OrderStatus.StockReserved;
    }

    public void MarkPaid(Guid paymentId)
    {
        EnsureCurrentStatusIs(OrderStatus.StockReserved, "mark payment as captured for");

        PaymentId = paymentId;
        Status = OrderStatus.Paid;
    }

    public void Confirm(DateTimeOffset confirmedOnUtc)
    {
        EnsureCurrentStatusIs(OrderStatus.Paid, "confirm");

        Status = OrderStatus.Confirmed;
        ConfirmedOnUtc = confirmedOnUtc;

        RaiseDomainEvent(new OrderConfirmedDomainEvent
        {
            OrderId = Id,
            CustomerId = CustomerId,
            GrandTotal = GrandTotal,
            DiscountTotal = DiscountTotal,
            TaxTotal = TaxTotal,
            ShippingTotal = ShippingTotal,
            PaymentMethod = PaymentMethod,
            Lines = _lines.ToList(),
            OccurredOnUtc = confirmedOnUtc,
        });
    }

    public void Cancel(string reason, DateTimeOffset occurredOnUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status is OrderStatus.Confirmed or OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            throw new OrderDomainException(
                $"Cannot cancel order {Id} because it is already {Status}.");
        }

        Status = OrderStatus.Cancelled;
        FailureReason = reason;
        CancelledOnUtc = occurredOnUtc;

        RaiseDomainEvent(new OrderCancelledDomainEvent(Id, CustomerId, reason, occurredOnUtc));
    }

    public void Refund(string reason, DateTimeOffset occurredOnUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        EnsureCurrentStatusIs(OrderStatus.Confirmed, "refund");

        Status = OrderStatus.Refunded;

        RaiseDomainEvent(new OrderRefundedDomainEvent(Id, GrandTotal, reason, occurredOnUtc));
    }

    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        var pendingDomainEvents = _domainEvents.ToList();
        _domainEvents.Clear();
        return pendingDomainEvents;
    }

    private static void EnsureIdentifiersArePresent(OrderSubmission submission)
    {
        if (submission.OrderId == Guid.Empty || submission.CustomerId == Guid.Empty)
        {
            throw new OrderDomainException("An order requires both an order and a customer identifier.");
        }
    }

    private static void EnsureCustomerContactIsPresent(OrderSubmission submission)
    {
        if (string.IsNullOrWhiteSpace(submission.CustomerEmail)
            || string.IsNullOrWhiteSpace(submission.CustomerDisplayName)
            || string.IsNullOrWhiteSpace(submission.PaymentMethod))
        {
            throw new OrderDomainException(
                "An order requires a customer email, a customer display name and a payment method.");
        }
    }

    private static void EnsureLinesAreOrderable(IReadOnlyList<OrderLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new OrderDomainException("An order requires at least one line.");
        }

        if (lines.Any(line => line.Quantity <= 0))
        {
            throw new OrderDomainException("Every order line requires a positive quantity.");
        }
    }

    private static void EnsureTotalsBalance(OrderSubmission submission)
    {
        var expectedGrandTotal = submission.Subtotal
            .Subtract(submission.DiscountTotal)
            .Add(submission.TaxTotal)
            .Add(submission.ShippingTotal);

        if (expectedGrandTotal != submission.GrandTotal)
        {
            throw new OrderDomainException(
                $"Order grand total {submission.GrandTotal.Amount} {submission.GrandTotal.CurrencyCode} does not equal "
                + "subtotal minus discount plus tax plus shipping "
                + $"({expectedGrandTotal.Amount} {expectedGrandTotal.CurrencyCode}).");
        }
    }

    private void EnsureCurrentStatusIs(OrderStatus expectedStatus, string action)
    {
        if (Status != expectedStatus)
        {
            throw new OrderDomainException(
                $"Cannot {action} order {Id} while it is {Status}; the order must be {expectedStatus}.");
        }
    }

    private void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
