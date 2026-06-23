using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Application.Projections.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FreshCart.Reporting.Tests.Projections;

public sealed class OrderConfirmedProjectionConsumerTests
{
    private readonly IProjectionInbox projectionInbox = Substitute.For<IProjectionInbox>();
    private readonly IProjectionWriter projectionWriter = Substitute.For<IProjectionWriter>();
    private readonly OrderConfirmedProjectionConsumer consumer;

    public OrderConfirmedProjectionConsumerTests()
    {
        consumer = new OrderConfirmedProjectionConsumer(
            projectionInbox,
            projectionWriter,
            NullLogger<OrderConfirmedProjectionConsumer>.Instance);
    }

    [Fact]
    public async Task AppliesProjectionThenRecordsEventAsProcessedOnFirstDelivery()
    {
        var integrationEvent = CreateOrderConfirmedEvent();
        using var cancellationSource = new CancellationTokenSource();
        var consumeContext = CreateConsumeContext(integrationEvent, cancellationSource.Token);

        projectionInbox
            .HasProcessedAsync(integrationEvent.EventId, cancellationSource.Token)
            .Returns(false);

        await consumer.Consume(consumeContext);

        await projectionWriter.Received(1).ApplyOrderConfirmedAsync(integrationEvent, cancellationSource.Token);
        await projectionInbox.Received(1).RecordProcessedAsync(integrationEvent.EventId, cancellationSource.Token);
    }

    [Fact]
    public async Task SkipsWarehouseWritesWhenEventIdWasAlreadyProcessedSoRedeliveriesDoNotDoubleCount()
    {
        var integrationEvent = CreateOrderConfirmedEvent();
        var consumeContext = CreateConsumeContext(integrationEvent, CancellationToken.None);

        projectionInbox
            .HasProcessedAsync(integrationEvent.EventId, Arg.Any<CancellationToken>())
            .Returns(true);

        await consumer.Consume(consumeContext);

        await projectionWriter.DidNotReceiveWithAnyArgs().ApplyOrderConfirmedAsync(default!, default);
        await projectionInbox.DidNotReceiveWithAnyArgs().RecordProcessedAsync(Guid.Empty, CancellationToken.None);
    }

    private static ConsumeContext<OrderConfirmedIntegrationEvent> CreateConsumeContext(
        OrderConfirmedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var consumeContext = Substitute.For<ConsumeContext<OrderConfirmedIntegrationEvent>>();
        consumeContext.Message.Returns(integrationEvent);
        consumeContext.CancellationToken.Returns(cancellationToken);
        return consumeContext;
    }

    private static OrderConfirmedIntegrationEvent CreateOrderConfirmedEvent() => new()
    {
        OrderId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        GrandTotal = 64.30m,
        DiscountTotal = 5.00m,
        TaxTotal = 4.30m,
        ShippingTotal = 6.00m,
        CurrencyCode = "USD",
        PaymentMethod = "Card",
        Lines =
        [
            new OrderConfirmedLine("SKU-APPLES-1KG", "Royal Gala Apples 1kg", "Produce", 2, 4.50m),
            new OrderConfirmedLine("SKU-MILK-2L", "Full Cream Milk 2L", "Dairy", 1, 3.80m),
        ],
    };
}
