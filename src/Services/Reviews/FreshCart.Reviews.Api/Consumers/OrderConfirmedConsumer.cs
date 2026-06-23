using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Persistence;
using MassTransit;

namespace FreshCart.Reviews.Api.Consumers;

/// <summary>
/// Turns a confirmed order into local purchase entitlements, one per line sku, so review authorisation
/// and the verified-purchase badge read from this service's own data rather than calling Ordering at
/// review time (event-carried state transfer). Each write is idempotent through the unique (CustomerId,
/// ProductSku, OrderId) index: a redelivered confirmation hits the duplicate-key path, which the
/// repository swallows, so re-consumption neither errors nor double-counts.
/// </summary>
public sealed partial class OrderConfirmedConsumer(
    IPurchaseRecordRepository purchaseRecordRepository,
    TimeProvider timeProvider,
    ILogger<OrderConfirmedConsumer> logger)
    : IConsumer<OrderConfirmedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderConfirmedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var orderConfirmed = context.Message;
        var purchasedOnUtc = timeProvider.GetUtcNow();

        var recordedSkuCount = 0;
        foreach (var line in orderConfirmed.Lines)
        {
            var purchaseRecord = PurchaseRecord.Record(
                Guid.CreateVersion7(),
                orderConfirmed.CustomerId,
                line.ProductSku,
                orderConfirmed.OrderId,
                purchasedOnUtc);

            if (await purchaseRecordRepository
                    .TryRecordAsync(purchaseRecord, context.CancellationToken)
                    .ConfigureAwait(false))
            {
                recordedSkuCount++;
            }
        }

        LogEntitlementsRecorded(orderConfirmed.OrderId, recordedSkuCount, orderConfirmed.Lines.Count);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Recorded {RecordedSkuCount} of {LineCount} purchase entitlements for order {OrderId}")]
    private partial void LogEntitlementsRecorded(Guid orderId, int recordedSkuCount, int lineCount);
}
