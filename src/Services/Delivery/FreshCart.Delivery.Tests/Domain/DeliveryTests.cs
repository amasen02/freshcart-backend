using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Delivery.Domain.Deliveries;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Tests.Domain;

public sealed class DeliveryTests
{
    private static readonly DateTimeOffset ScheduledOn = new(2026, 6, 18, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SlotStart = new(2026, 6, 19, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ASscheduledDeliveryGoesOutForDeliveryThenCompletes()
    {
        var delivery = CreateScheduledDelivery();
        var completedOn = SlotStart.AddHours(1);

        delivery.StartOutForDelivery();
        delivery.Complete(completedOn);

        delivery.Status.Should().Be(DeliveryStatus.Completed);
        delivery.CompletedOnUtc.Should().Be(completedOn);
    }

    [Fact]
    public void CompletingADeliveryThatNeverWentOutForDeliveryThrows()
    {
        var delivery = CreateScheduledDelivery();

        var completion = () => delivery.Complete(SlotStart.AddHours(1));

        completion.Should().Throw<DomainException>().WithMessage("*out for delivery can be completed*");
        delivery.Status.Should().Be(DeliveryStatus.Scheduled);
    }

    [Fact]
    public void GoingOutForDeliveryTwiceThrowsBecauseTheTransitionIsLinear()
    {
        var delivery = CreateScheduledDelivery();
        delivery.StartOutForDelivery();

        var secondTransition = delivery.StartOutForDelivery;

        secondTransition.Should().Throw<DomainException>().WithMessage("*scheduled delivery can go out*");
    }

    [Fact]
    public void AScheduledDeliveryCanBeFailed()
    {
        var delivery = CreateScheduledDelivery();
        var failedOn = SlotStart.AddHours(2);

        delivery.Fail(failedOn);

        delivery.Status.Should().Be(DeliveryStatus.Failed);
        delivery.CompletedOnUtc.Should().Be(failedOn);
    }

    [Fact]
    public void ACompletedDeliveryCannotBeFailed()
    {
        var delivery = CreateScheduledDelivery();
        delivery.StartOutForDelivery();
        delivery.Complete(SlotStart.AddHours(1));

        var failure = () => delivery.Fail(SlotStart.AddHours(3));

        failure.Should().Throw<DomainException>().WithMessage("*cannot be failed*");
    }

    private static DeliveryAggregate CreateScheduledDelivery() => DeliveryAggregate.Schedule(
        Guid.NewGuid(),
        Guid.NewGuid(),
        new DeliveryAddress("12 Market Street", null, "London", "SW1A 1AA", "GB"),
        SlotStart,
        SlotStart.AddHours(3),
        Guid.NewGuid(),
        ScheduledOn);
}
