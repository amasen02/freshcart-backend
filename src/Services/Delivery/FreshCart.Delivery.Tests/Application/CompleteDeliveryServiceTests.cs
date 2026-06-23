using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Application.Fulfilment;
using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Tests.Support;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Tests.Application;

public sealed class CompleteDeliveryServiceTests
{
    private static readonly Guid DeliveryId = Guid.Parse("12121212-1212-1212-1212-121212121212");
    private static readonly Guid OrderId = Guid.Parse("13131313-1313-1313-1313-131313131313");
    private static readonly Guid CustomerId = Guid.Parse("14141414-1414-1414-1414-141414141414");
    private static readonly DateTimeOffset Now = new(2026, 6, 19, 10, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SlotStart = new(2026, 6, 19, 9, 0, 0, TimeSpan.Zero);

    private readonly IDeliveryRepository deliveries = Substitute.For<IDeliveryRepository>();
    private readonly IPublishEndpoint publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly CompleteDeliveryService service;

    public CompleteDeliveryServiceTests() =>
        service = new CompleteDeliveryService(
            deliveries,
            publishEndpoint,
            new FixedTimeProvider(Now),
            NullLogger<CompleteDeliveryService>.Instance);

    [Fact]
    public async Task StartingOutForDeliveryMovesAScheduledDeliveryAndPersistsIt()
    {
        var delivery = ScheduledDelivery();
        deliveries.FindByIdAsync(DeliveryId, Arg.Any<CancellationToken>()).Returns(delivery);

        await service.StartOutForDeliveryAsync(DeliveryId, CancellationToken.None);

        delivery.Status.Should().Be(DeliveryStatus.OutForDelivery);
        await deliveries.Received(1).UpdateAsync(delivery, Arg.Any<CancellationToken>());
        await publishEndpoint.DidNotReceiveWithAnyArgs().Publish(default(DeliveryCompletedIntegrationEvent)!, default);
    }

    [Fact]
    public async Task CompletingADeliveryStampsTheClockTimeAndPublishesTheCompletedEvent()
    {
        var delivery = ScheduledDelivery();
        delivery.StartOutForDelivery();
        deliveries.FindByIdAsync(DeliveryId, Arg.Any<CancellationToken>()).Returns(delivery);

        await service.CompleteAsync(DeliveryId, CancellationToken.None);

        delivery.Status.Should().Be(DeliveryStatus.Completed);
        delivery.CompletedOnUtc.Should().Be(Now);
        await deliveries.Received(1).UpdateAsync(delivery, Arg.Any<CancellationToken>());
        await publishEndpoint.Received(1).Publish(
            Arg.Is<DeliveryCompletedIntegrationEvent>(completed =>
                completed.DeliveryId == DeliveryId
                && completed.OrderId == OrderId
                && completed.CustomerId == CustomerId
                && completed.DeliveredOnUtc == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompletingAnUnknownDeliveryThrowsNotFoundAndPublishesNothing()
    {
        deliveries.FindByIdAsync(DeliveryId, Arg.Any<CancellationToken>()).Returns((DeliveryAggregate?)null);

        var complete = async () => await service.CompleteAsync(DeliveryId, CancellationToken.None);

        await complete.Should().ThrowAsync<NotFoundException>();
        await publishEndpoint.DidNotReceiveWithAnyArgs().Publish(default(DeliveryCompletedIntegrationEvent)!, default);
    }

    [Fact]
    public async Task CompletingAScheduledDeliveryThatNeverWentOutThrowsAndPublishesNothing()
    {
        var delivery = ScheduledDelivery();
        deliveries.FindByIdAsync(DeliveryId, Arg.Any<CancellationToken>()).Returns(delivery);

        var complete = async () => await service.CompleteAsync(DeliveryId, CancellationToken.None);

        await complete.Should().ThrowAsync<DomainException>();
        await deliveries.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        await publishEndpoint.DidNotReceiveWithAnyArgs().Publish(default(DeliveryCompletedIntegrationEvent)!, default);
    }

    private static DeliveryAggregate ScheduledDelivery() => DeliveryAggregate.Rehydrate(
        DeliveryId,
        OrderId,
        CustomerId,
        new DeliveryAddress("3 Queen Street", null, "London", "WC2N 5DU", "GB"),
        DeliveryStatus.Scheduled,
        SlotStart,
        SlotStart.AddHours(3),
        Guid.NewGuid(),
        SlotStart.AddDays(-1),
        completedOnUtc: null);
}
