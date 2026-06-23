using FluentAssertions;
using FreshCart.Delivery.Domain.Scheduling;
using FreshCart.Delivery.Domain.Slots;

namespace FreshCart.Delivery.Tests.Domain;

public sealed class DeliverySchedulingPolicyTests
{
    private static readonly Guid ZoneId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset BaseInstant = new(2026, 6, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SelectsTheEarliestSlotThatStillHasFreeCapacity()
    {
        var earliest = SlotStartingAt(BaseInstant);
        var later = SlotStartingAt(BaseInstant.AddHours(3));

        var proposal = DeliverySchedulingPolicy.Propose([later, earliest], SingleDriverRotation());

        proposal.Should().NotBeNull();
        proposal!.Slot.Should().BeSameAs(earliest);
    }

    [Fact]
    public void RollsToTheNextSlotWhenTheEarliestSlotIsFull()
    {
        var fullEarliest = SlotStartingAt(BaseInstant, capacity: 1);
        fullEarliest.Book();
        var openLater = SlotStartingAt(BaseInstant.AddHours(3));

        var proposal = DeliverySchedulingPolicy.Propose([fullEarliest, openLater], SingleDriverRotation());

        proposal.Should().NotBeNull();
        proposal!.Slot.Should().BeSameAs(openLater);
    }

    [Fact]
    public void ReturnsNullWhenEverySlotIsExhausted()
    {
        var full = SlotStartingAt(BaseInstant, capacity: 1);
        full.Book();

        var proposal = DeliverySchedulingPolicy.Propose([full], SingleDriverRotation());

        proposal.Should().BeNull();
    }

    [Fact]
    public void ReturnsNullWhenNoActiveDriverExists()
    {
        var proposal = DeliverySchedulingPolicy.Propose([SlotStartingAt(BaseInstant)], activeDriverRotation: []);

        proposal.Should().BeNull();
    }

    [Fact]
    public void AssignsTheDriverThatWasLeastRecentlyAssigned()
    {
        var recentlyAssigned = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var staleAssigned = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

        var rotation = new[]
        {
            new DriverAssignment(recentlyAssigned, BaseInstant),
            new DriverAssignment(staleAssigned, BaseInstant.AddHours(-5)),
        };

        var proposal = DeliverySchedulingPolicy.Propose([SlotStartingAt(BaseInstant)], rotation);

        proposal.Should().NotBeNull();
        proposal!.DriverId.Should().Be(staleAssigned);
    }

    [Fact]
    public void PrefersANeverAssignedDriverOverAnyPreviouslyAssignedOne()
    {
        var neverAssigned = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
        var previouslyAssigned = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

        var rotation = new[]
        {
            new DriverAssignment(previouslyAssigned, BaseInstant.AddHours(-100)),
            new DriverAssignment(neverAssigned, LastAssignedOnUtc: null),
        };

        var proposal = DeliverySchedulingPolicy.Propose([SlotStartingAt(BaseInstant)], rotation);

        proposal!.DriverId.Should().Be(neverAssigned);
    }

    [Fact]
    public void RotatesDriversFairlyAcrossASequenceOfSchedulingDecisions()
    {
        var driverOne = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
        var driverTwo = Guid.Parse("cccccccc-0000-0000-0000-000000000002");
        var driverThree = Guid.Parse("cccccccc-0000-0000-0000-000000000003");

        var lastAssigned = new Dictionary<Guid, DateTimeOffset?>
        {
            [driverOne] = null,
            [driverTwo] = null,
            [driverThree] = null,
        };

        var assignmentOrder = new List<Guid>();
        var clock = BaseInstant;

        for (var iteration = 0; iteration < 6; iteration++)
        {
            var rotation = lastAssigned
                .Select(entry => new DriverAssignment(entry.Key, entry.Value))
                .ToList();

            var proposal = DeliverySchedulingPolicy.Propose([SlotStartingAt(clock)], rotation);
            proposal.Should().NotBeNull();

            assignmentOrder.Add(proposal!.DriverId);
            lastAssigned[proposal.DriverId] = clock;
            clock = clock.AddMinutes(10);
        }

        assignmentOrder.Take(3).Should().OnlyHaveUniqueItems(
            "each driver must be used once before any driver is reused");
        assignmentOrder.Skip(3).Take(3).Should().OnlyHaveUniqueItems(
            "the second full rotation must again touch every driver exactly once");
        assignmentOrder.Should().HaveCount(6);
        assignmentOrder.GroupBy(driverId => driverId)
            .Should().OnlyContain(group => group.Count() == 2,
                "across two rounds every driver must be assigned exactly twice");
    }

    private static DeliverySlot SlotStartingAt(DateTimeOffset startUtc, int capacity = 5) =>
        DeliverySlot.Create(ZoneId, startUtc, startUtc.AddHours(3), capacity);

    private static IReadOnlyList<DriverAssignment> SingleDriverRotation() =>
        [new DriverAssignment(Guid.Parse("99999999-9999-9999-9999-999999999999"), LastAssignedOnUtc: null)];
}
