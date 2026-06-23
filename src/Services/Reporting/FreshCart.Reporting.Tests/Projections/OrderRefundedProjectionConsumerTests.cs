using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Application.Projections.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FreshCart.Reporting.Tests.Projections;

public sealed class OrderRefundedProjectionConsumerTests
{
    private readonly IProjectionInbox projectionInbox = Substitute.For<IProjectionInbox>();
    private readonly IProjectionWriter projectionWriter = Substitute.For<IProjectionWriter>();
    private readonly OrderRefundedProjectionConsumer consumer;

    public OrderRefundedProjectionConsumerTests()
    {
        consumer = new OrderRefundedProjectionConsumer(
            projectionInbox,
            projectionWriter,
            NullLogger<OrderRefundedProjectionConsumer>.Instance);
    }

    [Fact]
    public async Task AppliesRefundProjectionThenRecordsEventAsProcessedOnFirstDelivery()
    {
        var integrationEvent = CreateOrderRefundedEvent();
        using var cancellationSource = new CancellationTokenSource();
        var consumeContext = CreateConsumeContext(integrationEvent, cancellationSource.Token);

        projectionInbox
            .HasProcessedAsync(integrationEvent.EventId, cancellationSource.Token)
            .Returns(false);

        await consumer.Consume(consumeContext);

        await projectionWriter.Received(1).ApplyOrderRefundedAsync(integrationEvent, cancellationSource.Token);
        await projectionInbox.Received(1).RecordProcessedAsync(integrationEvent.EventId, cancellationSource.Token);
    }

    [Fact]
    public async Task SkipsWarehouseWritesWhenEventIdWasAlreadyProcessedSoRedeliveriesDoNotDoubleRefund()
    {
        var integrationEvent = CreateOrderRefundedEvent();
        var consumeContext = CreateConsumeContext(integrationEvent, CancellationToken.None);

        projectionInbox
            .HasProcessedAsync(integrationEvent.EventId, Arg.Any<CancellationToken>())
            .Returns(true);

        await consumer.Consume(consumeContext);

        await projectionWriter.DidNotReceiveWithAnyArgs().ApplyOrderRefundedAsync(default!, default);
        await projectionInbox.DidNotReceiveWithAnyArgs().RecordProcessedAsync(Guid.Empty, CancellationToken.None);
    }

    private static ConsumeContext<OrderRefundedIntegrationEvent> CreateConsumeContext(
        OrderRefundedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var consumeContext = Substitute.For<ConsumeContext<OrderRefundedIntegrationEvent>>();
        consumeContext.Message.Returns(integrationEvent);
        consumeContext.CancellationToken.Returns(cancellationToken);
        return consumeContext;
    }

    private static OrderRefundedIntegrationEvent CreateOrderRefundedEvent() => new()
    {
        OrderId = Guid.NewGuid(),
        RefundAmount = 12.75m,
        CurrencyCode = "USD",
        Reason = "Damaged item reported by customer",
    };
}
