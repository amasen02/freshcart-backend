using FluentAssertions;
using FreshCart.Notification.Api.Channels;
using FreshCart.Notification.Api.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FreshCart.Notification.Tests.Channels;

public sealed class NotificationDispatcherTests
{
    private static readonly NotificationDocument Notification = new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        SourceEventId = Guid.NewGuid(),
        Type = NotificationTypes.OrderConfirmed,
        Title = "Order confirmed",
        Message = "Your order is confirmed.",
        OrderId = Guid.NewGuid(),
        CreatedOnUtc = DateTimeOffset.UnixEpoch,
        IsRead = false,
    };

    [Fact]
    public async Task ASecondChannelStillRunsAfterTheFirstChannelThrows()
    {
        var failingChannel = Substitute.For<INotificationChannel>();
        failingChannel
            .SendAsync(Notification, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("SignalR backplane unavailable."));
        var healthyChannel = Substitute.For<INotificationChannel>();

        var dispatcher = new NotificationDispatcher(
            [failingChannel, healthyChannel],
            NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.DispatchAsync(Notification, CancellationToken.None);

        await healthyChannel.Received(1).SendAsync(Notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EveryRegisteredChannelReceivesTheNotification()
    {
        var firstChannel = Substitute.For<INotificationChannel>();
        var secondChannel = Substitute.For<INotificationChannel>();

        var dispatcher = new NotificationDispatcher(
            [firstChannel, secondChannel],
            NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.DispatchAsync(Notification, CancellationToken.None);

        await firstChannel.Received(1).SendAsync(Notification, Arg.Any<CancellationToken>());
        await secondChannel.Received(1).SendAsync(Notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancellationIsPropagatedRatherThanSwallowedAsAChannelFailure()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        var cancelledChannel = Substitute.For<INotificationChannel>();
        cancelledChannel
            .SendAsync(Notification, Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException(cancellationSource.Token));

        var dispatcher = new NotificationDispatcher(
            [cancelledChannel],
            NullLogger<NotificationDispatcher>.Instance);

        var dispatch = () => dispatcher.DispatchAsync(Notification, cancellationSource.Token);

        await dispatch.Should().ThrowAsync<OperationCanceledException>();
    }
}
