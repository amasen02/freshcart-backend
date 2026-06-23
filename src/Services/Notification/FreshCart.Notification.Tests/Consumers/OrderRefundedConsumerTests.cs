using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Consumers;
using FreshCart.Notification.Api.Notifications;
using FreshCart.Notification.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FreshCart.Notification.Tests.Consumers;

public sealed class OrderRefundedConsumerTests
{
    private static readonly Guid CustomerId = Guid.Parse("99999999-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("99999999-0000-0000-0000-000000000002");

    private readonly NotificationConsumerHarness harness = new();
    private readonly OrderRefundedConsumer consumer;

    public OrderRefundedConsumerTests() =>
        consumer = new OrderRefundedConsumer(
            harness.Store,
            harness.Recorder,
            NullLogger<OrderRefundedConsumer>.Instance);

    [Fact]
    public async Task AddressesTheCustomerRecoveredFromTheEarlierOrderNotification()
    {
        SeedEarlierOrderNotification();
        var integrationEvent = OrderRefundedFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        var refundNotification = harness.Store.Stored
            .Should().ContainSingle(notification => notification.Type == NotificationTypes.OrderRefunded)
            .Which;
        refundNotification.UserId.Should().Be(CustomerId);
        refundNotification.OrderId.Should().Be(OrderId);
        refundNotification.Title.Should().Be("Refund issued");
        refundNotification.Message.Should().Contain("12.30 EUR");
        harness.DeliveredChannel.Delivered
            .Should().Contain(notification => notification.Type == NotificationTypes.OrderRefunded);
    }

    [Fact]
    public async Task ThrowsToTriggerRedeliveryWhenNoRecipientIsKnownYetForTheOrder()
    {
        var integrationEvent = OrderRefundedFor(Guid.NewGuid());

        var consume = () => consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        await consume.Should().ThrowAsync<InvalidOperationException>(
            "a missing recipient is a transient race, so the broker must redeliver rather than drop the notification");
        harness.Store.Stored.Should().BeEmpty();
        harness.DeliveredChannel.Delivered.Should().BeEmpty();
    }

    [Fact]
    public async Task ARedeliveredEventStoresAndDeliversTheRefundNotificationOnce()
    {
        SeedEarlierOrderNotification();
        var integrationEvent = OrderRefundedFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));
        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        harness.Store.Stored
            .Count(notification => string.Equals(notification.Type, NotificationTypes.OrderRefunded, StringComparison.Ordinal))
            .Should().Be(1);
    }

    private void SeedEarlierOrderNotification() =>
        harness.Store.Seed(new NotificationDocument
        {
            Id = Guid.NewGuid(),
            UserId = CustomerId,
            SourceEventId = Guid.NewGuid(),
            Type = NotificationTypes.OrderConfirmed,
            Title = "Order confirmed",
            Message = "Your order is confirmed.",
            OrderId = OrderId,
            CreatedOnUtc = NotificationConsumerHarness.FixedNowUtc.AddMinutes(-10),
            IsRead = false,
        });

    private static OrderRefundedIntegrationEvent OrderRefundedFor(Guid eventId) => new()
    {
        EventId = eventId,
        OrderId = OrderId,
        RefundAmount = 12.30m,
        CurrencyCode = "EUR",
        Reason = "Damaged on arrival.",
    };
}
