using FluentAssertions;
using FreshCart.Notification.Api.Notifications;
using Xunit;

namespace FreshCart.Notification.Tests.Notifications;

public sealed class NotificationDtoTests
{
    [Fact]
    public void MapsEveryFieldAndSerialisesTheTimestampAsRoundTripIso8601()
    {
        var notification = new NotificationDocument
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UserId = Guid.NewGuid(),
            SourceEventId = Guid.NewGuid(),
            Type = NotificationTypes.OrderConfirmed,
            Title = "Order confirmed",
            Message = "Your order is confirmed.",
            OrderId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            CreatedOnUtc = new DateTimeOffset(2026, 6, 18, 12, 30, 0, TimeSpan.Zero),
            IsRead = true,
        };

        var dto = NotificationDto.FromDocument(notification);

        dto.Id.Should().Be("33333333-3333-3333-3333-333333333333");
        dto.Type.Should().Be(NotificationTypes.OrderConfirmed);
        dto.Title.Should().Be("Order confirmed");
        dto.Message.Should().Be("Your order is confirmed.");
        dto.OrderId.Should().Be("44444444-4444-4444-4444-444444444444");
        dto.CreatedOnUtc.Should().Be("2026-06-18T12:30:00.0000000+00:00");
        dto.IsRead.Should().BeTrue();
    }

    [Fact]
    public void OrderIdIsNullOnTheWireWhenTheNotificationHasNoOrder()
    {
        var notification = new NotificationDocument
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SourceEventId = Guid.NewGuid(),
            Type = NotificationTypes.OrderPlaced,
            Title = "Welcome",
            Message = "Welcome to FreshCart.",
            OrderId = null,
            CreatedOnUtc = DateTimeOffset.UnixEpoch,
            IsRead = false,
        };

        var dto = NotificationDto.FromDocument(notification);

        dto.OrderId.Should().BeNull();
    }
}
