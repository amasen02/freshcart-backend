using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Consumers;
using FreshCart.Notification.Api.Notifications;
using FreshCart.Notification.Tests.Support;
using Xunit;

namespace FreshCart.Notification.Tests.Consumers;

public sealed class OrderPlacedConsumerTests
{
    private static readonly Guid CustomerId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    private readonly NotificationConsumerHarness harness = new();
    private readonly OrderPlacedConsumer consumer;

    public OrderPlacedConsumerTests() => consumer = new OrderPlacedConsumer(harness.Recorder);

    [Fact]
    public async Task StoresAndDeliversAnOrderReceivedNotificationAddressedToTheCustomer()
    {
        var integrationEvent = OrderPlacedFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        var stored = harness.Store.Stored.Should().ContainSingle().Which;
        stored.UserId.Should().Be(CustomerId);
        stored.OrderId.Should().Be(OrderId);
        stored.Type.Should().Be(NotificationTypes.OrderPlaced);
        stored.Title.Should().Be("Order received");
        harness.DeliveredChannel.Delivered.Should().ContainSingle().Which.Id.Should().Be(stored.Id);
    }

    [Fact]
    public async Task ARedeliveryWithTheSameEventIdStoresAndDeliversExactlyOnce()
    {
        var integrationEvent = OrderPlacedFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));
        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        harness.Store.Stored.Should().ContainSingle();
        harness.DeliveredChannel.Delivered.Should().ContainSingle();
    }

    private static OrderPlacedIntegrationEvent OrderPlacedFor(Guid eventId) => new()
    {
        EventId = eventId,
        OrderId = OrderId,
        CustomerId = CustomerId,
        CustomerEmail = "ada@example.com",
        CustomerDisplayName = "Ada",
        GrandTotal = 42.50m,
        CurrencyCode = "GBP",
    };
}
