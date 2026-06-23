using FluentAssertions;
using FreshCart.Notification.Api.Notifications;
using FreshCart.Notification.Tests.Support;
using Xunit;

namespace FreshCart.Notification.Tests.Notifications;

public sealed class NotificationRecorderTests
{
    private static readonly Guid UserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid SourceEventId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid OrderId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private readonly NotificationConsumerHarness harness = new();

    [Fact]
    public async Task FirstDeliveryStoresTheDocumentThenFansItOut()
    {
        var content = new NotificationContent(NotificationTypes.OrderPlaced, "Order received", "Thanks.");

        var outcome = await harness.Recorder.RecordAndDispatchAsync(
            UserId, SourceEventId, content, OrderId, CancellationToken.None);

        outcome.Should().Be(AddNotificationOutcome.Stored);
        harness.Store.Stored.Should().ContainSingle();
        harness.DeliveredChannel.Delivered.Should().ContainSingle();
    }

    [Fact]
    public async Task TheStoredDocumentTakesItsTimestampFromTheInjectedClockAndIsUnread()
    {
        var content = new NotificationContent(NotificationTypes.OrderPlaced, "Order received", "Thanks.");

        await harness.Recorder.RecordAndDispatchAsync(
            UserId, SourceEventId, content, OrderId, CancellationToken.None);

        var stored = harness.Store.Stored.Single();
        stored.CreatedOnUtc.Should().Be(NotificationConsumerHarness.FixedNowUtc);
        stored.IsRead.Should().BeFalse();
        stored.UserId.Should().Be(UserId);
        stored.SourceEventId.Should().Be(SourceEventId);
        stored.OrderId.Should().Be(OrderId);
    }

    [Fact]
    public async Task ARedeliveryWithTheSameEventIdStoresOnceAndDoesNotFanOutAgain()
    {
        var content = new NotificationContent(NotificationTypes.OrderPlaced, "Order received", "Thanks.");

        await harness.Recorder.RecordAndDispatchAsync(
            UserId, SourceEventId, content, OrderId, CancellationToken.None);
        var secondOutcome = await harness.Recorder.RecordAndDispatchAsync(
            UserId, SourceEventId, content, OrderId, CancellationToken.None);

        secondOutcome.Should().Be(AddNotificationOutcome.DuplicateIgnored);
        harness.Store.Stored.Should().ContainSingle();
        harness.DeliveredChannel.Delivered.Should().ContainSingle();
    }
}
