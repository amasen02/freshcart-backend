using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Application.Common.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Reporting.Application.Projections.Consumers;

/// <summary>
/// Projects the <see cref="DeliveryCompletedIntegrationEvent"/> emitted by the Delivery service onto the
/// scheduled <c>delivery_facts</c> row, stamping the completion time and deriving the on-time/late outcome
/// and duration. If the scheduled projection has not yet arrived the write finds no row and the writer
/// throws, so MassTransit redelivers until the slot is known.
/// </summary>
public sealed partial class DeliveryCompletedProjectionConsumer(
    IProjectionWriter projectionWriter,
    ILogger<DeliveryCompletedProjectionConsumer> logger)
    : IConsumer<DeliveryCompletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<DeliveryCompletedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var applied = await projectionWriter
            .ApplyDeliveryCompletedAsync(context.Message, context.CancellationToken)
            .ConfigureAwait(false);

        if (!applied)
        {
            LogSkippingAlreadyProcessedEvent(context.Message.EventId);
            return;
        }

        LogProjectedCompletedDelivery(context.Message.DeliveryId, context.Message.OrderId);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Skipping previously processed DeliveryCompletedIntegrationEvent {EventId}")]
    private partial void LogSkippingAlreadyProcessedEvent(Guid eventId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Projected completed delivery {DeliveryId} for order {OrderId}")]
    private partial void LogProjectedCompletedDelivery(Guid deliveryId, Guid orderId);
}
