using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Consumers;
using FreshCart.Notification.Api.Notifications;
using FreshCart.Notification.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FreshCart.Notification.Tests.Consumers;

public sealed class PaymentFailedConsumerTests
{
    private static readonly Guid CustomerId = Guid.Parse("ffffffff-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("ffffffff-0000-0000-0000-000000000002");

    private readonly NotificationConsumerHarness harness = new();
    private readonly PaymentFailedConsumer consumer;

    public PaymentFailedConsumerTests() =>
        consumer = new PaymentFailedConsumer(
            harness.Store,
            harness.Recorder,
            NullLogger<PaymentFailedConsumer>.Instance);

    [Fact]
    public async Task AddressesTheCustomerRecoveredFromTheEarlierOrderNotification()
    {
        SeedEarlierOrderNotification();
        var integrationEvent = PaymentFailedFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        var paymentNotification = harness.Store.Stored
            .Should().ContainSingle(notification => notification.Type == NotificationTypes.PaymentFailed)
            .Which;
        paymentNotification.UserId.Should().Be(CustomerId);
        paymentNotification.OrderId.Should().Be(OrderId);
        paymentNotification.Title.Should().Be("Payment could not be processed");
        harness.DeliveredChannel.Delivered
            .Should().Contain(notification => notification.Type == NotificationTypes.PaymentFailed);
    }

    [Fact]
    public async Task ThrowsToTriggerRedeliveryWhenNoRecipientIsKnownYetForTheOrder()
    {
        var integrationEvent = PaymentFailedFor(Guid.NewGuid());

        var consume = () => consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        await consume.Should().ThrowAsync<InvalidOperationException>(
            "a missing recipient is a transient race, so the broker must redeliver rather than drop the notification");
        harness.Store.Stored.Should().BeEmpty();
        harness.DeliveredChannel.Delivered.Should().BeEmpty();
    }

    [Fact]
    public async Task ARedeliveredEventStoresAndDeliversTheFailureNotificationOnce()
    {
        SeedEarlierOrderNotification();
        var integrationEvent = PaymentFailedFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));
        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        harness.Store.Stored
            .Count(notification => string.Equals(notification.Type, NotificationTypes.PaymentFailed, StringComparison.Ordinal))
            .Should().Be(1);
    }

    private void SeedEarlierOrderNotification() =>
        harness.Store.Seed(new NotificationDocument
        {
            Id = Guid.NewGuid(),
            UserId = CustomerId,
            SourceEventId = Guid.NewGuid(),
            Type = NotificationTypes.OrderPlaced,
            Title = "Order received",
            Message = "Thanks.",
            OrderId = OrderId,
            CreatedOnUtc = NotificationConsumerHarness.FixedNowUtc.AddMinutes(-5),
            IsRead = false,
        });

    private static PaymentFailedIntegrationEvent PaymentFailedFor(Guid eventId) => new()
    {
        EventId = eventId,
        OrderId = OrderId,
        PaymentId = Guid.NewGuid(),
        Reason = "The card issuer declined the charge.",
    };
}
