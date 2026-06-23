using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Consumers;
using FreshCart.Notification.Api.Notifications;
using FreshCart.Notification.Tests.Support;
using Xunit;

namespace FreshCart.Notification.Tests.Consumers;

public sealed class DeliveryScheduledConsumerTests
{
    private static readonly Guid CustomerId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("dddddddd-0000-0000-0000-000000000002");

    private readonly NotificationConsumerHarness harness = new();
    private readonly DeliveryScheduledConsumer consumer;

    public DeliveryScheduledConsumerTests() => consumer = new DeliveryScheduledConsumer(harness.Recorder);

    [Fact]
    public async Task StoresAndDeliversADeliveryScheduledNotificationWithTheSlotInTheMessage()
    {
        var integrationEvent = DeliveryScheduledFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        var stored = harness.Store.Stored.Should().ContainSingle().Which;
        stored.UserId.Should().Be(CustomerId);
        stored.OrderId.Should().Be(OrderId);
        stored.Type.Should().Be(NotificationTypes.DeliveryScheduled);
        stored.Title.Should().Be("Delivery scheduled");
        stored.Message.Should().Contain("09:00").And.Contain("11:00");
        harness.DeliveredChannel.Delivered.Should().ContainSingle();
    }

    [Fact]
    public async Task ARedeliveryWithTheSameEventIdStoresAndDeliversExactlyOnce()
    {
        var integrationEvent = DeliveryScheduledFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));
        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        harness.Store.Stored.Should().ContainSingle();
        harness.DeliveredChannel.Delivered.Should().ContainSingle();
    }

    private static DeliveryScheduledIntegrationEvent DeliveryScheduledFor(Guid eventId) => new()
    {
        EventId = eventId,
        OrderId = OrderId,
        DeliveryId = Guid.NewGuid(),
        CustomerId = CustomerId,
        SlotStartUtc = new DateTimeOffset(2026, 6, 20, 9, 0, 0, TimeSpan.Zero),
        SlotEndUtc = new DateTimeOffset(2026, 6, 20, 11, 0, 0, TimeSpan.Zero),
    };
}
