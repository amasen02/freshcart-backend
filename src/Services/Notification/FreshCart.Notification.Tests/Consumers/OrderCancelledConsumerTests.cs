using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Consumers;
using FreshCart.Notification.Api.Notifications;
using FreshCart.Notification.Tests.Support;
using Xunit;

namespace FreshCart.Notification.Tests.Consumers;

public sealed class OrderCancelledConsumerTests
{
    private static readonly Guid CustomerId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    private readonly NotificationConsumerHarness harness = new();
    private readonly OrderCancelledConsumer consumer;

    public OrderCancelledConsumerTests() => consumer = new OrderCancelledConsumer(harness.Recorder);

    [Fact]
    public async Task StoresAndDeliversAnOrderCancelledNotificationAddressedToTheCustomer()
    {
        var integrationEvent = OrderCancelledFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        var stored = harness.Store.Stored.Should().ContainSingle().Which;
        stored.UserId.Should().Be(CustomerId);
        stored.OrderId.Should().Be(OrderId);
        stored.Type.Should().Be(NotificationTypes.OrderCancelled);
        stored.Title.Should().Be("Order cancelled");
        stored.Message.Should().Contain("Requested by customer.");
        harness.DeliveredChannel.Delivered.Should().ContainSingle();
    }

    [Fact]
    public async Task ARedeliveryWithTheSameEventIdStoresAndDeliversExactlyOnce()
    {
        var integrationEvent = OrderCancelledFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));
        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        harness.Store.Stored.Should().ContainSingle();
        harness.DeliveredChannel.Delivered.Should().ContainSingle();
    }

    private static OrderCancelledIntegrationEvent OrderCancelledFor(Guid eventId) => new()
    {
        EventId = eventId,
        OrderId = OrderId,
        CustomerId = CustomerId,
        Reason = "Requested by customer.",
    };
}
