namespace FreshCart.Delivery.Application.Scheduling;

/// <summary>
/// The three terminal states of a scheduling attempt. Only <see cref="Scheduled"/> causes the consumer
/// to publish a <c>DeliveryScheduledIntegrationEvent</c>.
/// </summary>
public enum ScheduleDeliveryResult
{
    Scheduled = 0,
    AlreadyScheduled = 1,
    SkippedNotDeliverable = 2,
}
