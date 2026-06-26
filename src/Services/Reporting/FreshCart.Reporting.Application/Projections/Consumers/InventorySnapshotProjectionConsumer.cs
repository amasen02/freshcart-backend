using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Application.Common.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Reporting.Application.Projections.Consumers;

/// <summary>
/// Projects the <see cref="ProductCreatedIntegrationEvent"/> emitted by the Catalog service into the
/// <c>inventory_snapshot</c> table the inventory-health dashboard reads, seeding the SKU with its initial
/// on-hand quantity and unit cost.
/// </summary>
public sealed partial class InventorySnapshotProjectionConsumer(
    IProjectionWriter projectionWriter,
    ILogger<InventorySnapshotProjectionConsumer> logger)
    : IConsumer<ProductCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var applied = await projectionWriter
            .ApplyProductCreatedAsync(context.Message, context.CancellationToken)
            .ConfigureAwait(false);

        if (!applied)
        {
            LogSkippingAlreadyProcessedEvent(context.Message.EventId);
            return;
        }

        LogProjectedInventorySnapshot(context.Message.ProductSku, context.Message.InitialStockQuantity);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Skipping previously processed ProductCreatedIntegrationEvent {EventId}")]
    private partial void LogSkippingAlreadyProcessedEvent(Guid eventId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Projected inventory snapshot for {ProductSku} with {OnHand} on hand")]
    private partial void LogProjectedInventorySnapshot(string productSku, int onHand);
}
