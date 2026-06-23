using FluentAssertions;
using FreshCart.Notification.Api.Channels;
using FreshCart.Notification.Api.Notifications;
using Xunit;

namespace FreshCart.Notification.Tests.Channels;

public sealed class PlainTextEmailRendererTests
{
    private static readonly Guid OrderId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void SubjectIsTheNotificationTitleAndBodyOpensWithTheMessage()
    {
        var notification = NotificationFor("Order confirmed", "Your order is confirmed.", orderId: null);

        var email = PlainTextEmailRenderer.Render(notification);

        email.Subject.Should().Be("Order confirmed");
        email.PlainTextBody.Should().StartWith("Your order is confirmed.");
    }

    [Fact]
    public void BodyIncludesTheOrderReferenceWhenTheNotificationCarriesAnOrder()
    {
        var notification = NotificationFor("Order confirmed", "Your order is confirmed.", OrderId);

        var email = PlainTextEmailRenderer.Render(notification);

        email.PlainTextBody.Should().Contain($"Order reference: {OrderId}");
    }

    [Fact]
    public void BodyOmitsTheOrderReferenceLineWhenThereIsNoOrder()
    {
        var notification = NotificationFor("Welcome", "Welcome to FreshCart.", orderId: null);

        var email = PlainTextEmailRenderer.Render(notification);

        email.PlainTextBody.Should().NotContain("Order reference:");
    }

    [Fact]
    public void BodyEndsWithTheTeamSignature()
    {
        var notification = NotificationFor("Order confirmed", "Your order is confirmed.", OrderId);

        var email = PlainTextEmailRenderer.Render(notification);

        email.PlainTextBody.Should().EndWith("The FreshCart team");
    }

    private static NotificationDocument NotificationFor(string title, string message, Guid? orderId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        SourceEventId = Guid.NewGuid(),
        Type = NotificationTypes.OrderConfirmed,
        Title = title,
        Message = message,
        OrderId = orderId,
        CreatedOnUtc = DateTimeOffset.UnixEpoch,
        IsRead = false,
    };
}
