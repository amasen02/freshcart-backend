using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Notifications;
using Xunit;

namespace FreshCart.Notification.Tests.Notifications;

public sealed class NotificationContentFactoryTests
{
    [Fact]
    public void OrderPlacedGreetsTheCustomerByNameAndStatesTheFormattedTotal()
    {
        var integrationEvent = new OrderPlacedIntegrationEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CustomerEmail = "ada@example.com",
            CustomerDisplayName = "Ada",
            GrandTotal = 42.5m,
            CurrencyCode = "GBP",
        };

        var content = NotificationContentFactory.ForOrderPlaced(integrationEvent);

        content.Type.Should().Be(NotificationTypes.OrderPlaced);
        content.Title.Should().Be("Order received");
        content.Message.Should().Contain("Ada").And.Contain("42.50 GBP");
    }

    [Fact]
    public void OrderConfirmedReportsTheItemCountAndRoundedTotal()
    {
        var integrationEvent = new OrderConfirmedIntegrationEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            GrandTotal = 64.305m,
            DiscountTotal = 5.00m,
            TaxTotal = 4.30m,
            ShippingTotal = 6.00m,
            CurrencyCode = "USD",
            PaymentMethod = "Card",
            Lines =
            [
                new OrderConfirmedLine("SKU-APPLES-1KG", "Royal Gala Apples 1kg", "Produce", 2, 4.50m),
                new OrderConfirmedLine("SKU-MILK-2L", "Full Cream Milk 2L", "Dairy", 1, 3.80m),
            ],
        };

        var content = NotificationContentFactory.ForOrderConfirmed(integrationEvent);

        content.Type.Should().Be(NotificationTypes.OrderConfirmed);
        content.Title.Should().Be("Order confirmed");
        content.Message.Should().Contain("3 item(s)").And.Contain("64.30 USD");
    }

    [Fact]
    public void PaymentFailedSurfacesTheDeclineReasonAndAsksToUpdateDetails()
    {
        var integrationEvent = new PaymentFailedIntegrationEvent
        {
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            Reason = "The card issuer declined the charge.",
        };

        var content = NotificationContentFactory.ForPaymentFailed(integrationEvent);

        content.Type.Should().Be(NotificationTypes.PaymentFailed);
        content.Title.Should().Be("Payment could not be processed");
        content.Message
            .Should().Contain("The card issuer declined the charge.")
            .And.Contain("update your payment details");
    }

    [Fact]
    public void OrderCancelledCarriesTheCancellationReason()
    {
        var integrationEvent = new OrderCancelledIntegrationEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Reason = "Requested by customer.",
        };

        var content = NotificationContentFactory.ForOrderCancelled(integrationEvent);

        content.Type.Should().Be(NotificationTypes.OrderCancelled);
        content.Title.Should().Be("Order cancelled");
        content.Message.Should().Contain("Requested by customer.");
    }

    [Fact]
    public void OrderRefundedStatesTheFormattedRefundAndTheReason()
    {
        var integrationEvent = new OrderRefundedIntegrationEvent
        {
            OrderId = Guid.NewGuid(),
            RefundAmount = 12.3m,
            CurrencyCode = "EUR",
            Reason = "Damaged on arrival.",
        };

        var content = NotificationContentFactory.ForOrderRefunded(integrationEvent);

        content.Type.Should().Be(NotificationTypes.OrderRefunded);
        content.Title.Should().Be("Refund issued");
        content.Message.Should().Contain("12.30 EUR").And.Contain("Damaged on arrival.");
    }

    [Fact]
    public void DeliveryScheduledFormatsBothEndsOfTheSlotInTheMessage()
    {
        var integrationEvent = new DeliveryScheduledIntegrationEvent
        {
            OrderId = Guid.NewGuid(),
            DeliveryId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            SlotStartUtc = new DateTimeOffset(2026, 6, 20, 9, 0, 0, TimeSpan.Zero),
            SlotEndUtc = new DateTimeOffset(2026, 6, 20, 11, 0, 0, TimeSpan.Zero),
        };

        var content = NotificationContentFactory.ForDeliveryScheduled(integrationEvent);

        content.Type.Should().Be(NotificationTypes.DeliveryScheduled);
        content.Title.Should().Be("Delivery scheduled");
        content.Message.Should().Contain("09:00").And.Contain("11:00");
    }

    [Fact]
    public void DeliveryCompletedStatesWhenTheOrderWasDelivered()
    {
        var integrationEvent = new DeliveryCompletedIntegrationEvent
        {
            OrderId = Guid.NewGuid(),
            DeliveryId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            DeliveredOnUtc = new DateTimeOffset(2026, 6, 20, 10, 15, 0, TimeSpan.Zero),
        };

        var content = NotificationContentFactory.ForDeliveryCompleted(integrationEvent);

        content.Type.Should().Be(NotificationTypes.DeliveryCompleted);
        content.Title.Should().Be("Delivery completed");
        content.Message.Should().Contain("10:15");
    }
}
