using FreshCart.Notification.Api.Channels;
using FreshCart.Notification.Api.Notifications;
using FreshCart.Notification.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FreshCart.Notification.Tests.Support;

/// <summary>
/// Wires a real <see cref="NotificationRecorder"/> over the in-memory store and a real dispatcher
/// driving a recording channel, so consumer tests assert the genuine store-then-fan-out behaviour
/// (including the unique-index idempotency rule) without a database or transport.
/// </summary>
public sealed class NotificationConsumerHarness
{
    public static readonly DateTimeOffset FixedNowUtc = new(2026, 6, 18, 9, 0, 0, TimeSpan.Zero);

    public NotificationConsumerHarness()
    {
        Store = new InMemoryNotificationStore();
        DeliveredChannel = new RecordingNotificationChannel();

        var dispatcher = new NotificationDispatcher(
            [DeliveredChannel],
            NullLogger<NotificationDispatcher>.Instance);

        Recorder = new NotificationRecorder(
            Store,
            dispatcher,
            new FixedTimeProvider(FixedNowUtc),
            NullLogger<NotificationRecorder>.Instance);
    }

    public InMemoryNotificationStore Store { get; }

    public RecordingNotificationChannel DeliveredChannel { get; }

    public NotificationRecorder Recorder { get; }
}
