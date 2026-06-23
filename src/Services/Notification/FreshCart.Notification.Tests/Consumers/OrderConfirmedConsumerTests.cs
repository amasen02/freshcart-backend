using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Consumers;
using FreshCart.Notification.Api.Hubs;
using FreshCart.Notification.Api.Notifications;
using FreshCart.Notification.Tests.Support;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Xunit;

namespace FreshCart.Notification.Tests.Consumers;

public sealed class OrderConfirmedConsumerTests
{
    private static readonly Guid CustomerId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private readonly NotificationConsumerHarness harness = new();
    private readonly IHubContext<NotificationHub> hubContext = Substitute.For<IHubContext<NotificationHub>>();
    private readonly IClientProxy backOfficeProxy = Substitute.For<IClientProxy>();
    private readonly OrderConfirmedConsumer consumer;

    public OrderConfirmedConsumerTests()
    {
        var hubClients = Substitute.For<IHubClients>();
        hubContext.Clients.Returns(hubClients);
        hubClients.Group(NotificationGroups.BackOffice).Returns(backOfficeProxy);

        consumer = new OrderConfirmedConsumer(harness.Recorder, hubContext);
    }

    [Fact]
    public async Task StoresTheCustomerNotificationAndTicksTheBackOfficeDashboard()
    {
        var integrationEvent = OrderConfirmedFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        var stored = harness.Store.Stored.Should().ContainSingle().Which;
        stored.UserId.Should().Be(CustomerId);
        stored.Type.Should().Be(NotificationTypes.OrderConfirmed);
        stored.Title.Should().Be("Order confirmed");

        await backOfficeProxy.Received(1).SendCoreAsync(
            NotificationHubMethods.SalesDashboardUpdated,
            Arg.Is<object?[]>(arguments => arguments.Length == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ARedeliveredEventNeitherStoresAgainNorReticksTheDashboard()
    {
        var integrationEvent = OrderConfirmedFor(Guid.NewGuid());

        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));
        await consumer.Consume(ConsumeContextFactory.For(integrationEvent, CancellationToken.None));

        harness.Store.Stored.Should().ContainSingle();
        await backOfficeProxy.Received(1).SendCoreAsync(
            NotificationHubMethods.SalesDashboardUpdated,
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    private static OrderConfirmedIntegrationEvent OrderConfirmedFor(Guid eventId) => new()
    {
        EventId = eventId,
        OrderId = OrderId,
        CustomerId = CustomerId,
        GrandTotal = 64.30m,
        DiscountTotal = 5.00m,
        TaxTotal = 4.30m,
        ShippingTotal = 6.00m,
        CurrencyCode = "USD",
        PaymentMethod = "Card",
        Lines =
        [
            new OrderConfirmedLine("SKU-APPLES-1KG", "Royal Gala Apples 1kg", "Produce", 2, 4.50m),
            new OrderConfirmedLine("SKU-MILK-2L", "Full Cream Milk 2L", "Dairy", 1, 3.80m),
        ],
    };
}
