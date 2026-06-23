using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Consumers;
using FreshCart.Notification.Api.Notifications;
using FreshCart.Notification.Tests.Support;
using Xunit;

namespace FreshCart.Notification.Tests.Consumers;

public sealed class DeliveryCompletedConsumerTests
{
    private static readonly Guid CustomerId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000002");

    private readonly NotificationConsumerHarness harness = new();
    private readonly DeliveryCompletedConsumer consumer;

    public DeliveryCompletedConsumerTests() => consumer = new DeliveryCompletedConsumer(harness.Recorder);

    [Fact]
    public async Task StoresAndDeliversADeliveryCompletedNotificationAddressedToTheCustomer()
    {
        var integrationEvent = DeliveryCompletedFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        var stored = harness.Store.Stored.Should().ContainSingle().Which;
        stored.UserId.Should().Be(CustomerId);
        stored.OrderId.Should().Be(OrderId);
        stored.Type.Should().Be(NotificationTypes.DeliveryCompleted);
        stored.Title.Should().Be("Delivery completed");
        harness.DeliveredChannel.Delivered.Should().ContainSingle();
    }

    [Fact]
    public async Task ARedeliveryWithTheSameEventIdStoresAndDeliversExactlyOnce()
    {
        var integrationEvent = DeliveryCompletedFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));
        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        harness.Store.Stored.Should().ContainSingle();
        harness.DeliveredChannel.Delivered.Should().ContainSingle();
    }

    private static DeliveryCompletedIntegrationEvent DeliveryCompletedFor(Guid eventId) => new()
    {
        EventId = eventId,
        OrderId = OrderId,
        DeliveryId = Guid.NewGuid(),
        CustomerId = CustomerId,
        DeliveredOnUtc = new DateTimeOffset(2026, 6, 20, 10, 15, 0, TimeSpan.Zero),
    };
}
