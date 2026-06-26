using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Application.Projections.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FreshCart.Reporting.Tests.Projections;

public sealed class InventorySnapshotProjectionConsumerTests
{
    private readonly IProjectionWriter projectionWriter = Substitute.For<IProjectionWriter>();
    private readonly InventorySnapshotProjectionConsumer consumer;

    public InventorySnapshotProjectionConsumerTests() =>
        consumer = new InventorySnapshotProjectionConsumer(
            projectionWriter,
            NullLogger<InventorySnapshotProjectionConsumer>.Instance);

    [Fact]
    public async Task AppliesProjectionOnFirstDelivery()
    {
        var integrationEvent = CreateProductCreatedEvent();
        projectionWriter.ApplyProductCreatedAsync(integrationEvent, Arg.Any<CancellationToken>()).Returns(true);

        await consumer.Consume(CreateConsumeContext(integrationEvent));

        await projectionWriter.Received(1).ApplyProductCreatedAsync(integrationEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TreatsAnAlreadyProcessedEventAsANoOp()
    {
        var integrationEvent = CreateProductCreatedEvent();
        projectionWriter.ApplyProductCreatedAsync(integrationEvent, Arg.Any<CancellationToken>()).Returns(false);

        await consumer.Consume(CreateConsumeContext(integrationEvent));

        await projectionWriter.Received(1).ApplyProductCreatedAsync(integrationEvent, Arg.Any<CancellationToken>());
    }

    private static ConsumeContext<ProductCreatedIntegrationEvent> CreateConsumeContext(ProductCreatedIntegrationEvent integrationEvent)
    {
        var consumeContext = Substitute.For<ConsumeContext<ProductCreatedIntegrationEvent>>();
        consumeContext.Message.Returns(integrationEvent);
        consumeContext.CancellationToken.Returns(CancellationToken.None);
        return consumeContext;
    }

    private static ProductCreatedIntegrationEvent CreateProductCreatedEvent() => new()
    {
        ProductId = Guid.NewGuid(),
        ProductSku = "SKU-WIDGET-1",
        ProductName = "Widget",
        PrimaryCategory = "Widgets",
        BasePrice = 9.99m,
        CurrencyCode = "USD",
        InitialStockQuantity = 100,
        IsDigital = false,
    };
}
