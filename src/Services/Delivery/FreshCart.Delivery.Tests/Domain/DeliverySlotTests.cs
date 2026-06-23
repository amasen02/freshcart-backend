using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Delivery.Domain.Slots;

namespace FreshCart.Delivery.Tests.Domain;

public sealed class DeliverySlotTests
{
    private static readonly Guid ZoneId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset SlotStart = new(2026, 6, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BookingIncrementsTheBookedCountWhileCapacityRemains()
    {
        var slot = DeliverySlot.Create(ZoneId, SlotStart, SlotStart.AddHours(3), capacity: 2);

        slot.Book();

        slot.BookedCount.Should().Be(1);
        slot.HasFreeCapacity.Should().BeTrue();
    }

    [Fact]
    public void BookingTheLastUnitRemovesFreeCapacity()
    {
        var slot = DeliverySlot.Create(ZoneId, SlotStart, SlotStart.AddHours(3), capacity: 1);

        slot.Book();

        slot.BookedCount.Should().Be(1);
        slot.HasFreeCapacity.Should().BeFalse();
    }

    [Fact]
    public void BookingAFullSlotThrowsAndLeavesTheCountUnchanged()
    {
        var slot = DeliverySlot.Create(ZoneId, SlotStart, SlotStart.AddHours(3), capacity: 1);
        slot.Book();

        var booking = slot.Book;

        booking.Should().Throw<DomainException>()
            .WithMessage($"*{slot.Id}*full*");
        slot.BookedCount.Should().Be(1);
    }

    [Fact]
    public void CreatingASlotThatEndsBeforeItStartsThrows()
    {
        var creation = () => DeliverySlot.Create(ZoneId, SlotStart, SlotStart.AddHours(-1), capacity: 5);

        creation.Should().Throw<DomainException>().WithMessage("*end after it starts*");
    }

    [Fact]
    public void CreatingASlotWithNonPositiveCapacityThrows()
    {
        var creation = () => DeliverySlot.Create(ZoneId, SlotStart, SlotStart.AddHours(3), capacity: 0);

        creation.Should().Throw<DomainException>().WithMessage("*capacity must be positive*");
    }

    [Fact]
    public void RehydratingWithABookedCountAboveCapacityThrows()
    {
        var rehydrate = () => DeliverySlot.Rehydrate(
            Guid.NewGuid(),
            ZoneId,
            SlotStart,
            SlotStart.AddHours(3),
            capacity: 5,
            bookedCount: 6);

        rehydrate.Should().Throw<DomainException>().WithMessage("*booked count must be within*");
    }
}
