using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Application.Tracking;
using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Domain.Slots;
using NSubstitute;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Tests.Application;

public sealed class DeliveryTrackingQueriesTests
{
    private static readonly Guid OrderId = Guid.Parse("15151515-1515-1515-1515-151515151515");
    private static readonly Guid OwnerId = Guid.Parse("16161616-1616-1616-1616-161616161616");
    private static readonly Guid OtherCustomerId = Guid.Parse("17171717-1717-1717-1717-171717171717");
    private static readonly Guid ZoneId = Guid.Parse("18181818-1818-1818-1818-181818181818");
    private static readonly DateTimeOffset SlotStart = new(2026, 6, 20, 9, 0, 0, TimeSpan.Zero);

    private readonly IDeliveryRepository deliveries = Substitute.For<IDeliveryRepository>();
    private readonly ISlotRepository slots = Substitute.For<ISlotRepository>();
    private readonly DeliveryTrackingQueries queries;

    public DeliveryTrackingQueriesTests() => queries = new DeliveryTrackingQueries(deliveries, slots);

    [Fact]
    public async Task ReturnsTrackingForTheOwningCustomer()
    {
        deliveries.FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>()).Returns(OwnedDelivery());

        var tracking = await queries.GetTrackingForOrderAsync(
            OrderId,
            OwnerId,
            requesterIsAdministrator: false,
            CancellationToken.None);

        tracking.OrderId.Should().Be(OrderId);
        tracking.CustomerId.Should().Be(OwnerId);
        tracking.Address.PostalCode.Should().Be("WC2N 5DU");
    }

    [Fact]
    public async Task AllowsAnAdministratorToTrackAnotherCustomersDelivery()
    {
        deliveries.FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>()).Returns(OwnedDelivery());

        var tracking = await queries.GetTrackingForOrderAsync(
            OrderId,
            OtherCustomerId,
            requesterIsAdministrator: true,
            CancellationToken.None);

        tracking.CustomerId.Should().Be(OwnerId);
    }

    [Fact]
    public Task ForbidsADifferentCustomerFromTrackingSomeoneElsesDelivery()
    {
        deliveries.FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>()).Returns(OwnedDelivery());

        Func<Task> track = () => queries.GetTrackingForOrderAsync(
            OrderId,
            OtherCustomerId,
            requesterIsAdministrator: false,
            CancellationToken.None);

        return track.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public Task ThrowsNotFoundWhenNoDeliveryExistsForTheOrder()
    {
        deliveries.FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>()).Returns((DeliveryAggregate?)null);

        Func<Task> track = () => queries.GetTrackingForOrderAsync(
            OrderId,
            OwnerId,
            requesterIsAdministrator: false,
            CancellationToken.None);

        return track.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ListsOpenSlotsSortedByStartWithRemainingCapacity()
    {
        var partlyBooked = DeliverySlot.Create(ZoneId, SlotStart.AddHours(3), SlotStart.AddHours(6), capacity: 5);
        partlyBooked.Book();
        partlyBooked.Book();
        var fresh = DeliverySlot.Create(ZoneId, SlotStart, SlotStart.AddHours(3), capacity: 5);

        slots.ListOpenSlotsOnDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([partlyBooked, fresh]);

        var openSlots = await queries.ListOpenSlotsAsync(
            DateOnly.FromDateTime(SlotStart.UtcDateTime),
            CancellationToken.None);

        openSlots.Should().HaveCount(2);
        openSlots[0].StartUtc.Should().Be(SlotStart);
        openSlots[0].RemainingCapacity.Should().Be(5);
        openSlots[1].RemainingCapacity.Should().Be(3);
    }

    private static DeliveryAggregate OwnedDelivery() => DeliveryAggregate.Rehydrate(
        Guid.NewGuid(),
        OrderId,
        OwnerId,
        new DeliveryAddress("3 Queen Street", null, "London", "WC2N 5DU", "GB"),
        DeliveryStatus.Scheduled,
        SlotStart,
        SlotStart.AddHours(3),
        Guid.NewGuid(),
        SlotStart.AddDays(-1),
        completedOnUtc: null);
}
