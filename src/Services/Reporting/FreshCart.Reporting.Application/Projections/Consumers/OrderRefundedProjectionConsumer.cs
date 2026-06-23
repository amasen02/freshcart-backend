using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Application.Common.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Reporting.Application.Projections.Consumers;

/// <summary>
/// Projects the <see cref="OrderRefundedIntegrationEvent"/> emitted by the Ordering service into
/// the read warehouse, reducing net revenue on the affected sales fact.
/// </summary>
public sealed partial class OrderRefundedProjectionConsumer(
    IProjectionInbox projectionInbox,
    IProjectionWriter projectionWriter,
    ILogger<OrderRefundedProjectionConsumer> logger)
    : IConsumer<OrderRefundedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderRefundedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var alreadyProcessed = await projectionInbox
            .HasProcessedAsync(context.Message.EventId, context.CancellationToken)
            .ConfigureAwait(false);

        if (alreadyProcessed)
        {
            LogSkippingAlreadyProcessedEvent(context.Message.EventId);
            return;
        }

        await projectionWriter
            .ApplyOrderRefundedAsync(context.Message, context.CancellationToken)
            .ConfigureAwait(false);

        await projectionInbox
            .RecordProcessedAsync(context.Message.EventId, context.CancellationToken)
            .ConfigureAwait(false);

        LogProjectedOrderRefunded(
            context.Message.OrderId,
            context.Message.RefundAmount,
            context.Message.CurrencyCode);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Skipping previously processed OrderRefundedIntegrationEvent {EventId}")]
    private partial void LogSkippingAlreadyProcessedEvent(Guid eventId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Projected OrderRefunded for order {OrderId} totalling {RefundAmount} {CurrencyCode}")]
    private partial void LogProjectedOrderRefunded(Guid orderId, decimal refundAmount, string currencyCode);
}
