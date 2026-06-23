using FreshCart.Delivery.Domain.Deliveries;

namespace FreshCart.Delivery.Application.Shipments;

/// <summary>
/// Local projection of the shipping facts a delivery needs but <c>OrderConfirmedIntegrationEvent</c>
/// does not carry. It is built from <c>BasketCheckoutStartedIntegrationEvent</c> (which owns the
/// address and the line composition) and joined against when the order is later confirmed. This is the
/// deliberate alternative to fattening the shared OrderConfirmed contract: a service builds the local
/// state it needs from the upstream events it already receives. The row is deleted once scheduling
/// consumes it.
/// </summary>
public sealed class PendingShipment
{
    public PendingShipment(Guid orderId, Guid customerId, DeliveryAddress? shippingAddress, bool hasPhysicalLines)
    {
        OrderId = orderId;
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        HasPhysicalLines = hasPhysicalLines;
    }

    public Guid OrderId { get; }

    public Guid CustomerId { get; }

    /// <summary>
    /// The address to deliver to. Null when the checkout carried no shipping address, which together
    /// with a digital-only basket means the order is not physically deliverable.
    /// </summary>
    public DeliveryAddress? ShippingAddress { get; }

    public bool HasPhysicalLines { get; }

    /// <summary>
    /// An order is deliverable only when it has at least one physical line and a shipping address to
    /// send it to. Digital-only checkouts are intentionally skipped without raising an error.
    /// </summary>
    public bool IsDeliverable => HasPhysicalLines && ShippingAddress is not null;
}
