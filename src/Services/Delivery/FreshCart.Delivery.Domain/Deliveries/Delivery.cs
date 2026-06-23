using System.Globalization;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Delivery.Domain.Deliveries;

/// <summary>
/// Physical fulfilment of a confirmed order. Holds the chosen slot window and assigned driver and
/// enforces a linear lifecycle: Scheduled to OutForDelivery to Completed or Failed. Every transition is
/// a method on the aggregate so the status field can never be moved into an illegal state from outside.
/// </summary>
public sealed class Delivery
{
    private Delivery(
        Guid id,
        Guid orderId,
        Guid customerId,
        DeliveryAddress address,
        DeliveryStatus status,
        DateTimeOffset slotStartUtc,
        DateTimeOffset slotEndUtc,
        Guid? driverId,
        DateTimeOffset createdOnUtc,
        DateTimeOffset? completedOnUtc)
    {
        Id = id;
        OrderId = orderId;
        CustomerId = customerId;
        Address = address;
        Status = status;
        SlotStartUtc = slotStartUtc;
        SlotEndUtc = slotEndUtc;
        DriverId = driverId;
        CreatedOnUtc = createdOnUtc;
        CompletedOnUtc = completedOnUtc;
    }

    public Guid Id { get; }

    public Guid OrderId { get; }

    public Guid CustomerId { get; }

    public DeliveryAddress Address { get; }

    public DeliveryStatus Status { get; private set; }

    public DateTimeOffset SlotStartUtc { get; }

    public DateTimeOffset SlotEndUtc { get; }

    public Guid? DriverId { get; }

    public DateTimeOffset CreatedOnUtc { get; }

    public DateTimeOffset? CompletedOnUtc { get; private set; }

    public static Delivery Schedule(
        Guid orderId,
        Guid customerId,
        DeliveryAddress address,
        DateTimeOffset slotStartUtc,
        DateTimeOffset slotEndUtc,
        Guid driverId,
        DateTimeOffset scheduledOnUtc)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (slotEndUtc <= slotStartUtc)
        {
            throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"A delivery slot must end after it starts; received start {slotStartUtc:O} and end {slotEndUtc:O}."));
        }

        return new Delivery(
            Guid.CreateVersion7(),
            orderId,
            customerId,
            address,
            DeliveryStatus.Scheduled,
            slotStartUtc,
            slotEndUtc,
            driverId,
            scheduledOnUtc,
            completedOnUtc: null);
    }

    public static Delivery Rehydrate(
        Guid id,
        Guid orderId,
        Guid customerId,
        DeliveryAddress address,
        DeliveryStatus status,
        DateTimeOffset slotStartUtc,
        DateTimeOffset slotEndUtc,
        Guid? driverId,
        DateTimeOffset createdOnUtc,
        DateTimeOffset? completedOnUtc)
    {
        ArgumentNullException.ThrowIfNull(address);

        return new Delivery(
            id,
            orderId,
            customerId,
            address,
            status,
            slotStartUtc,
            slotEndUtc,
            driverId,
            createdOnUtc,
            completedOnUtc);
    }

    public void StartOutForDelivery()
    {
        if (Status != DeliveryStatus.Scheduled)
        {
            throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"Only a scheduled delivery can go out for delivery; delivery {Id} is {Status}."));
        }

        Status = DeliveryStatus.OutForDelivery;
    }

    public void Complete(DateTimeOffset completedOnUtc)
    {
        if (Status != DeliveryStatus.OutForDelivery)
        {
            throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"Only a delivery that is out for delivery can be completed; delivery {Id} is {Status}."));
        }

        Status = DeliveryStatus.Completed;
        CompletedOnUtc = completedOnUtc;
    }

    public void Fail(DateTimeOffset failedOnUtc)
    {
        if (Status is DeliveryStatus.Completed or DeliveryStatus.Failed)
        {
            throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"A {Status} delivery cannot be failed; delivery {Id} is already terminal."));
        }

        Status = DeliveryStatus.Failed;
        CompletedOnUtc = failedOnUtc;
    }
}
