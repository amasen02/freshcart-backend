namespace FreshCart.Delivery.Application.Shipments;

/// <summary>
/// Raised when an order is confirmed before its <c>BasketCheckoutStartedIntegrationEvent</c> has been
/// projected into a <see cref="PendingShipment"/>. The two events travel independent routes, so this
/// ordering race is expected: the consumer lets the exception escape so the MassTransit retry policy
/// redelivers the OrderConfirmed message after the pending shipment has landed, rather than dropping a
/// delivery that should have been scheduled.
/// </summary>
public sealed class PendingShipmentNotYetAvailableException : Exception
{
    public PendingShipmentNotYetAvailableException(Guid orderId)
        : base($"No pending shipment is available yet for order \"{orderId}\"; the OrderConfirmed event will be retried.")
    {
        OrderId = orderId;
    }

    public Guid OrderId { get; }
}
