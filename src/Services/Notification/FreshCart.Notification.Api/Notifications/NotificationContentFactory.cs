using System.Globalization;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

namespace FreshCart.Notification.Api.Notifications;

/// <summary>
/// Turns each integration event into the wording shown to the customer. Centralising the copy keeps
/// the consumers free of presentation concerns and gives one place to assert the wording.
/// </summary>
public static class NotificationContentFactory
{
    private const string SlotTimeFormat = "ddd d MMM, HH:mm";

    public static NotificationContent ForOrderPlaced(OrderPlacedIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var total = FormatMoney(integrationEvent.GrandTotal, integrationEvent.CurrencyCode);
        return new NotificationContent(
            NotificationTypes.OrderPlaced,
            "Order received",
            $"Thanks {integrationEvent.CustomerDisplayName}, we have received your order for {total} and are getting it ready.");
    }

    public static NotificationContent ForOrderConfirmed(OrderConfirmedIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var total = FormatMoney(integrationEvent.GrandTotal, integrationEvent.CurrencyCode);
        var itemCount = integrationEvent.Lines.Sum(line => line.Quantity);
        return new NotificationContent(
            NotificationTypes.OrderConfirmed,
            "Order confirmed",
            $"Your order is confirmed: {itemCount} item(s) totalling {total}. We will let you know when it is on its way.");
    }

    public static NotificationContent ForPaymentFailed(PaymentFailedIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return new NotificationContent(
            NotificationTypes.PaymentFailed,
            "Payment could not be processed",
            $"We could not process the payment for your order. {integrationEvent.Reason} Please update your payment details to continue.");
    }

    public static NotificationContent ForOrderCancelled(OrderCancelledIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return new NotificationContent(
            NotificationTypes.OrderCancelled,
            "Order cancelled",
            $"Your order has been cancelled. {integrationEvent.Reason}");
    }

    public static NotificationContent ForOrderRefunded(OrderRefundedIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var refund = FormatMoney(integrationEvent.RefundAmount, integrationEvent.CurrencyCode);
        return new NotificationContent(
            NotificationTypes.OrderRefunded,
            "Refund issued",
            $"A refund of {refund} has been issued for your order. {integrationEvent.Reason}");
    }

    public static NotificationContent ForDeliveryScheduled(DeliveryScheduledIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var slotStart = FormatSlot(integrationEvent.SlotStartUtc);
        var slotEnd = FormatSlot(integrationEvent.SlotEndUtc);
        return new NotificationContent(
            NotificationTypes.DeliveryScheduled,
            "Delivery scheduled",
            $"Your delivery is booked for {slotStart} to {slotEnd}. We will be in touch on the day.");
    }

    public static NotificationContent ForDeliveryCompleted(DeliveryCompletedIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var deliveredOn = FormatSlot(integrationEvent.DeliveredOnUtc);
        return new NotificationContent(
            NotificationTypes.DeliveryCompleted,
            "Delivery completed",
            $"Your order was delivered on {deliveredOn}. We hope you enjoy your groceries.");
    }

    private static string FormatMoney(decimal amount, string currencyCode)
    {
        var rounded = Math.Round(amount, 2, MidpointRounding.ToEven);
        return string.Create(CultureInfo.InvariantCulture, $"{rounded:0.00} {currencyCode}");
    }

    private static string FormatSlot(DateTimeOffset instant) =>
        instant.ToString(SlotTimeFormat, CultureInfo.InvariantCulture);
}
