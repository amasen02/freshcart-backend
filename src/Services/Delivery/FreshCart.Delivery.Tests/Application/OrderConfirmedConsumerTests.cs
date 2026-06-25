using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.Events;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Application.Scheduling;
using FreshCart.Delivery.Application.Shipments;
using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Domain.Drivers;
using FreshCart.Delivery.Domain.Scheduling;
using FreshCart.Delivery.Domain.Slots;
using FreshCart.Delivery.Domain.Zones;
using FreshCart.Delivery.Tests.Support;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Tests.Application;

public sealed class OrderConfirmedConsumerTests
{
    private static readonly Guid OrderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CustomerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ZoneId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid DriverId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SlotStart = new(2026, 6, 19, 9, 0, 0, TimeSpan.Zero);

    private readonly IPendingShipmentRepository pendingShipments = Substitute.For<IPendingShipmentRepository>();
    private readonly IDeliveryRepository deliveries = Substitute.For<IDeliveryRepository>();
    private readonly IDeliveryUnitOfWork unitOfWork = Substitute.For<IDeliveryUnitOfWork>();
    private readonly ISlotRepository slots = Substitute.For<ISlotRepository>();
    private readonly IZoneRepository zones = Substitute.For<IZoneRepository>();
    private readonly IDriverRepository drivers = Substitute.For<IDriverRepository>();
    private readonly IGeocodingService geocoding = Substitute.For<IGeocodingService>();
    private readonly FixedTimeProvider timeProvider = new(Now);
    private readonly OrderConfirmedConsumer consumer;

    public OrderConfirmedConsumerTests()
    {
        var schedulingService = new ScheduleDeliveryService(
            pendingShipments,
            deliveries,
            unitOfWork,
            slots,
            zones,
            drivers,
            geocoding,
            timeProvider,
            NullLogger<ScheduleDeliveryService>.Instance);

        consumer = new OrderConfirmedConsumer(
            schedulingService,
            NullLogger<OrderConfirmedConsumer>.Instance);
    }

    [Fact]
    public async Task SchedulesADeliveryAndStagesTheScheduledEventForADeliverableOrder()
    {
        var openSlot = DeliverySlot.Create(ZoneId, SlotStart, SlotStart.AddHours(3), capacity: 5);
        ArrangeDeliverableOrder(openSlot);

        await consumer.Consume(CreateContext());

        await slots.Received(1).TryBookSlotAsync(
            Arg.Is<DeliverySlot>(slot => slot.Id == openSlot.Id),
            Arg.Any<CancellationToken>());
        await pendingShipments.Received(1).DeleteByOrderIdAsync(OrderId, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).PersistScheduledDeliveryAsync(
            Arg.Is<DeliveryAggregate>(delivery => delivery.OrderId == OrderId && delivery.DriverId == DriverId),
            Arg.Is<IntegrationEvent>(integrationEvent =>
                integrationEvent is DeliveryScheduledIntegrationEvent
                && ((DeliveryScheduledIntegrationEvent)integrationEvent).OrderId == OrderId
                && ((DeliveryScheduledIntegrationEvent)integrationEvent).CustomerId == CustomerId
                && ((DeliveryScheduledIntegrationEvent)integrationEvent).SlotStartUtc == SlotStart),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipsADigitalOnlyOrderWithoutSchedulingOrStaging()
    {
        deliveries.FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>()).Returns((DeliveryAggregate?)null);
        pendingShipments.FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(new PendingShipment(OrderId, CustomerId, shippingAddress: null, hasPhysicalLines: false));

        await consumer.Consume(CreateContext());

        await unitOfWork.DidNotReceiveWithAnyArgs().PersistScheduledDeliveryAsync(default!, default!, default);
        await pendingShipments.Received(1).DeleteByOrderIdAsync(OrderId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ThrowsRetriableWhenThePendingShipmentHasNotArrivedYet()
    {
        deliveries.FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>()).Returns((DeliveryAggregate?)null);
        pendingShipments.FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>()).Returns((PendingShipment?)null);

        var consume = async () => await consumer.Consume(CreateContext());

        await consume.Should().ThrowAsync<PendingShipmentNotYetAvailableException>();
        await unitOfWork.DidNotReceiveWithAnyArgs().PersistScheduledDeliveryAsync(default!, default!, default);
    }

    [Fact]
    public async Task ThrowsRetriableAndSchedulesNothingWhenTheProposedSlotWasBookedByARace()
    {
        var openSlot = DeliverySlot.Create(ZoneId, SlotStart, SlotStart.AddHours(3), capacity: 5);
        ArrangeDeliverableOrder(openSlot);
        slots.TryBookSlotAsync(Arg.Any<DeliverySlot>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var consume = async () => await consumer.Consume(CreateContext());

        await consume.Should().ThrowAsync<SlotNoLongerAvailableException>();
        await unitOfWork.DidNotReceiveWithAnyArgs().PersistScheduledDeliveryAsync(default!, default!, default);
        await pendingShipments.DidNotReceiveWithAnyArgs().DeleteByOrderIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsIdempotentWhenADeliveryAlreadyExistsForTheOrder()
    {
        var existing = DeliveryAggregate.Schedule(
            OrderId,
            CustomerId,
            new DeliveryAddress("1 High Street", null, "London", "SW1A 1AA", "GB"),
            SlotStart,
            SlotStart.AddHours(3),
            DriverId,
            Now);
        deliveries.FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>()).Returns(existing);

        await consumer.Consume(CreateContext());

        await unitOfWork.DidNotReceiveWithAnyArgs().PersistScheduledDeliveryAsync(default!, default!, default);
        await pendingShipments.DidNotReceiveWithAnyArgs()
            .FindByOrderIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private void ArrangeDeliverableOrder(DeliverySlot openSlot)
    {
        deliveries.FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>()).Returns((DeliveryAggregate?)null);

        var address = new DeliveryAddress("1 High Street", null, "London", "SW1A 1AA", "GB");
        pendingShipments.FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(new PendingShipment(OrderId, CustomerId, address, hasPhysicalLines: true));

        geocoding.Locate(Arg.Any<DeliveryAddress>()).Returns(new GeoCoordinate(51.51, -0.12));

        var zone = DeliveryZone.Rehydrate(ZoneId, "City Centre", SquarePolygon());
        zones.FindZoneContainingAsync(Arg.Any<GeoCoordinate>(), Arg.Any<CancellationToken>()).Returns(zone);

        slots.ListOpenSlotsForZoneAsync(ZoneId, Arg.Any<CancellationToken>())
            .Returns([openSlot]);
        slots.TryBookSlotAsync(Arg.Any<DeliverySlot>(), Arg.Any<CancellationToken>())
            .Returns(true);
        drivers.GetActiveDriverRotationAsync(Arg.Any<CancellationToken>())
            .Returns([new DriverAssignment(DriverId, LastAssignedOnUtc: null)]);
    }

    private static ConsumeContext<OrderConfirmedIntegrationEvent> CreateContext()
    {
        var integrationEvent = new OrderConfirmedIntegrationEvent
        {
            OrderId = OrderId,
            CustomerId = CustomerId,
            GrandTotal = 42.00m,
            DiscountTotal = 0m,
            TaxTotal = 2.00m,
            ShippingTotal = 5.00m,
            CurrencyCode = "GBP",
            PaymentMethod = "Card",
            Lines = [new OrderConfirmedLine("SKU-1", "Bananas", "Produce", 3, 1.20m)],
        };

        var context = Substitute.For<ConsumeContext<OrderConfirmedIntegrationEvent>>();
        context.Message.Returns(integrationEvent);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    private static ZonePolygon SquarePolygon()
    {
        var corner = new GeoCoordinate(51.50, -0.15);
        return new ZonePolygon(
        [
            corner,
            new GeoCoordinate(51.50, -0.08),
            new GeoCoordinate(51.54, -0.08),
            new GeoCoordinate(51.54, -0.15),
            corner,
        ]);
    }
}
