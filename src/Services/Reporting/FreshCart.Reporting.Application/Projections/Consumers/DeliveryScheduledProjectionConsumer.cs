using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Application.Common.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Reporting.Application.Projections.Consumers;

/// <summary>
/// Projects the <see cref="DeliveryScheduledIntegrationEvent"/> emitted by the Delivery service into the
/// <c>delivery_facts</c> table, recording the scheduled slot. The completion event later fills in the
/// outcome and duration against this row.
/// </summary>
public sealed partial class DeliveryScheduledProjectionConsumer(
    IProjectionWriter projectionWriter,
    ILogger<DeliveryScheduledProjectionConsumer> logger)
    : IConsumer<DeliveryScheduledIntegrationEvent>
{
    public async Task Consume(ConsumeContext<DeliveryScheduledIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var applied = await projectionWriter
            .ApplyDeliveryScheduledAsync(context.Message, context.CancellationToken)
            .ConfigureAwait(false);

        if (!applied)
        {
            LogSkippingAlreadyProcessedEvent(context.Message.EventId);
            return;
        }

        LogProjectedScheduledDelivery(context.Message.DeliveryId, context.Message.OrderId);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Skipping previously processed DeliveryScheduledIntegrationEvent {EventId}")]
    private partial void LogSkippingAlreadyProcessedEvent(Guid eventId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Projected scheduled delivery {DeliveryId} for order {OrderId}")]
    private partial void LogProjectedScheduledDelivery(Guid deliveryId, Guid orderId);
}
