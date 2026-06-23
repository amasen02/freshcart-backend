using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Application.Scheduling;

/// <summary>
/// Result of attempting to schedule a delivery for a confirmed order. Distinguishes the three honest
/// outcomes the consumer must react to differently: a delivery was created, an existing delivery meant
/// the request was a duplicate, or the order was not physically deliverable and was skipped.
/// </summary>
public sealed record ScheduleDeliveryOutcome
{
    private ScheduleDeliveryOutcome(ScheduleDeliveryResult result, DeliveryAggregate? delivery)
    {
        Result = result;
        Delivery = delivery;
    }

    public ScheduleDeliveryResult Result { get; }

    public DeliveryAggregate? Delivery { get; }

    public static ScheduleDeliveryOutcome Scheduled(DeliveryAggregate delivery)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        return new ScheduleDeliveryOutcome(ScheduleDeliveryResult.Scheduled, delivery);
    }

    public static ScheduleDeliveryOutcome AlreadyScheduled(DeliveryAggregate existingDelivery)
    {
        ArgumentNullException.ThrowIfNull(existingDelivery);
        return new ScheduleDeliveryOutcome(ScheduleDeliveryResult.AlreadyScheduled, existingDelivery);
    }

    public static ScheduleDeliveryOutcome SkippedNotDeliverable { get; } =
        new(ScheduleDeliveryResult.SkippedNotDeliverable, delivery: null);
}
