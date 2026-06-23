using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Inventory.Api.Services;
using MassTransit;

namespace FreshCart.Inventory.Api.Consumers;

public sealed partial class ProductCreatedConsumer(
    IStockLevelService stockLevelService,
    ILogger<ProductCreatedConsumer> logger) : IConsumer<ProductCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var productCreated = context.Message;

        // Idempotent on redelivery: ProductCreated is at-least-once, so this must only seed the row on
        // first sight. Overwriting (SetStockLevel) would reset QuantityOnHand and wipe real stock movement.
        var created = await stockLevelService
            .EnsureStockItemAsync(
                productCreated.ProductSku,
                productCreated.ProductName,
                productCreated.InitialStockQuantity,
                context.CancellationToken)
            .ConfigureAwait(false);

        LogStockRowEnsured(productCreated.ProductSku, productCreated.InitialStockQuantity, created);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Stock row ensured for product {ProductSku} with initial quantity {InitialStockQuantity} (created: {Created})")]
    private partial void LogStockRowEnsured(string productSku, int initialStockQuantity, bool created);
}
